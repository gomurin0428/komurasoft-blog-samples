#!/usr/bin/env bash
# 記事「GigE Vision のパケットを Ada の型で受ける」のコードを確かめる。
# CI では alr が環境（GNAT・gprbuild・GNATprove）を用意する。
# alr が無い環境では PATH 上の gprbuild を使い、GNATprove の段階は飛ばす。
set -euo pipefail
cd "$(dirname "$0")"

if command -v alr >/dev/null 2>&1; then
  RUN=(alr -n exec --)
  alr -n build
else
  RUN=()
  gprbuild -q -p -P gvsp.gpr
fi

"${RUN[@]}" gcc --version | head -1
./bin/byte_order_demo
./bin/overlay_demo
./bin/gvsp_test

# Scalar_Storage_Order の型を番地で重ねると、GNAT は警告を出すはず。
"${RUN[@]}" gcc -c -gnatc -Isrc warn/overlay_warning.adb 2>&1 | tee warn.log
rm -f overlay_warning.ali
grep -q 'overlay changes scalar storage order' warn.log

# 拡張 ID の長さの確認を消した版をテストにつなぐと、12バイトの拡張 ID のパケットで止まるはず。
# -gnata ありでは Read_U64 の事前条件、なしでは関数の中の添字の検査で例外になる。
run_bad() {
  local mode=$1 pattern=$2 out
  "${RUN[@]}" gprbuild -q -p -P bad/bad_run.gpr -XASSERT="$mode"
  if out=$("./bad/obj-run-$mode/gvsp_test" 2>&1); then
    echo "$out"
    echo "UNEXPECTED SUCCESS: unchecked version (ASSERT=$mode)"
    exit 1
  fi
  echo "$out" | tail -2
  grep -qF -- "$pattern" <<<"$out"
}
run_bad on  'raised ADA.ASSERTIONS.ASSERTION_ERROR : failed precondition from gvsp.ads:47'
run_bad off 'raised CONSTRAINT_ERROR : gvsp.adb:8 index check failed'

if [ ${#RUN[@]} -eq 0 ]; then
  echo "SKIP gnatprove (alr not found)"
  echo "ALL CHECKS PASSED (without gnatprove)"
  exit 0
fi

"${RUN[@]}" gnatprove --version

# 長さを確かめる版は、すべての検査を証明できるはず。
"${RUN[@]}" gnatprove -P gvsp.gpr -u gvsp.adb --level=2 --report=all --checks-as-errors=on -j0

# 拡張 ID の長さの確認を消した版は、証明できないはず。
set +e
"${RUN[@]}" gnatprove -P bad/gvsp_unchecked.gpr -u gvsp.adb --level=2 \
  --report=fail --checks-as-errors=on -j0 2>&1 | tee bad-prove.log
bad_status=${PIPESTATUS[0]}
set -e
if [ "$bad_status" -eq 0 ]; then
  echo "unexpected: the unchecked version was proved"
  exit 1
fi
grep -q 'precondition might fail' bad-prove.log
echo "the unchecked version was rejected as expected"
echo "ALL CHECKS PASSED"
