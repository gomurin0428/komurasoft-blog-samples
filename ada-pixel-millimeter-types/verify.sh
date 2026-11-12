#!/usr/bin/env bash
# 記事「ピクセルとミリメートルを型で分ける」のコードを確かめる。
# CI では alr が環境（GNAT・gprbuild・GNATprove）を用意する。
# alr が無い環境では PATH 上の gprbuild を使い、GNATprove の段階は飛ばす。
set -euo pipefail
cd "$(dirname "$0")"

if command -v alr >/dev/null 2>&1; then
  alr -n build
  RUN=(alr -n exec --)
else
  RUN=()
fi

"${RUN[@]}" gcc --version | head -1
"${RUN[@]}" gprbuild -q -P units.gpr

for m in naive_check measure_width tolerance_check small_check dim_check area_check; do
  echo "== $m"
  "./bin/$m"
done

# 記事に載せた出力と同じになること（pipefail で SIGPIPE を拾わないよう、出力を変数に受ける）
expect_output () {
  local exe=$1 pattern=$2 out
  out=$("./bin/$exe")
  if ! grep -qF -- "$pattern" <<<"$out"; then
    echo "MISSING OUTPUT in $exe: $pattern"
    exit 1
  fi
}

expect_output tolerance_check 'Long_Float の比較     : FALSE'
expect_output tolerance_check 'Mm に丸めてから比較   : TRUE'
expect_output tolerance_check 'Mm (X)       = 5.099'
expect_output tolerance_check "Mm'Round (X) = 5.100"
expect_output measure_width   'width  = 1.02100000000000E+01 mm'
expect_output dim_check       '10.210 mm'

# エラーの例は、コンパイルに失敗し、決まったメッセージを出すこと
expect_error () {
  local file=$1 pattern=$2 out
  echo "== $file"
  if out=$("${RUN[@]}" gprbuild -q -c -u -P errors.gpr "$file" 2>&1); then
    echo "$out"
    echo "UNEXPECTED SUCCESS: $file"
    exit 1
  fi
  echo "$out"
  if ! grep -qF -- "$pattern" <<<"$out"; then
    echo "MISSING MESSAGE in $file: $pattern"
    exit 1
  fi
}

expect_error err_px_times_px.adb        'cannot call abstract operation "*"'
expect_error err_forgot_scale.adb       'expected type "Millimeters"'
expect_error err_position_as_length.adb 'left operand has type "Position"'
expect_error err_center.adb             'cannot call abstract operation "+"'
expect_error err_inherited_ops.adb      'err_inherited_ops.adb:9:46: error: cannot call abstract operation "**"'
expect_error err_inherited_ops.adb      'err_inherited_ops.adb:10:51: error: expected type "Position"'
expect_error err_inherited_ops.adb      'err_inherited_ops.adb:11:58: error: cannot call abstract operation "**"'
expect_error err_y_with_x_scale.adb     'invalid operand types for operator "*"'
expect_error err_inverse_scale.adb      'invalid operand types for operator "*"'
expect_error err_dim.adb                'expected dimension [L], found [L**(-1).X**2]'

# 型で防げないもの: 型のないリテラルの係数は警告なしで通る（-gnatwe でビルド）
"${RUN[@]}" gprbuild -q -p -P limits.gpr
expect_output hole_literals ' 1.02100000000000E+03 5.00000000000000E+01 6.00000000000000E+00 FALSE'
# ピクセルの長さを Accept_Rounded に渡すとエラーになる
if out=$("${RUN[@]}" gprbuild -q -c -u -P limits.gpr hole_px.adb 2>&1); then
  echo "UNEXPECTED SUCCESS: hole_px.adb"
  exit 1
fi
echo "$out"
grep -qF 'expected type "Millimeters"' <<<"$out"

# 10進ではない固定小数点型への型変換（GNAT では切り捨てになる）
"${RUN[@]}" gprbuild -q -p -P trunc.gpr
trunc=$(./bin/trunc_run)
echo "$trunc"
[ "$trunc" = "$(printf ' 0.001\n 10.205')" ] || { echo "UNEXPECTED trunc_run output"; exit 1; }

# 範囲を広げた版は、実行時に Constraint_Error になる
"${RUN[@]}" gprbuild -q -P prove_wide.gpr
./bin/wide_run 16384.0 1.0
if ./bin/wide_run 1000000.0 1.0; then
  echo "UNEXPECTED SUCCESS: wide_run 1000000.0 1.0"
  exit 1
fi

# GNATprove（CI だけで実行する）
if [ ${#RUN[@]} -eq 0 ]; then
  echo "SKIP gnatprove (alr not found)"
  exit 0
fi

"${RUN[@]}" gnatprove --version

echo "== gnatprove prove.gpr"
"${RUN[@]}" gnatprove -P prove.gpr --level=2 --report=all -j0 --checks-as-errors=on

echo "== gnatprove prove_wide.gpr (failure expected)"
if out=$("${RUN[@]}" gnatprove -P prove_wide.gpr --level=2 --report=all -j0 --checks-as-errors=on 2>&1); then
  echo "$out"
  echo "UNEXPECTED SUCCESS: gnatprove prove_wide.gpr"
  exit 1
fi
echo "$out"
grep -qF 'range check might fail' <<<"$out"

echo "ALL CHECKS PASSED"
