#!/usr/bin/env bash
# 記事「画像処理の境界外アクセスを SPARK で証明する」のコードを確かめる。
# 1. ビルドと試験（Ada と C の結果の一致、乱数の画像での別の方法との比較、
#    仮ラベルが足りなくなる画像、重なった引数の検出、検査を外したビルド）
# 2. GNATprove で img_proof.gpr のすべての検査が証明されること
# 3. GNATprove で bug.gpr（わざと誤りを入れた版）の添字の検査が証明されないこと
# CI では alr が環境（GNAT・gprbuild・GNATprove）を用意する。
# alr が無い環境では PATH 上の gprbuild を使い、GNATprove の段階は飛ばす。
set -uo pipefail
cd "$(dirname "$0")"
fail=0

if command -v alr >/dev/null 2>&1; then
  RUN=(alr -n exec --)
  alr -n build || exit 1
else
  RUN=()
  gprbuild -q -p -P img_proof.gpr || exit 1
fi

"${RUN[@]}" gcc --version | head -1

echo "=== test_ops"
./bin/test-O2/test_ops || fail=1

echo "=== extra tests"
"${RUN[@]}" gprbuild -q -p -P extra.gpr || exit 1
./bin/extra/oracle || fail=1
./bin/extra/stair || fail=1
./bin/extra/alias_check || fail=1

# 検査を外したビルドでも、証明していない呼び出し側からの空の画像を事前条件で弾くこと
echo "=== test_ops (BUILD=nochecks)"
"${RUN[@]}" gprbuild -q -p -P img_proof.gpr -XBUILD=nochecks -XOPT=-O2 || fail=1
./bin/nochecks-O2/test_ops || fail=1

if [ ${#RUN[@]} -eq 0 ]; then
  echo "SKIP gnatprove (alr not found)"
  [ "$fail" -eq 0 ] && echo "ALL CHECKS PASSED (without gnatprove)"
  exit $fail
fi

"${RUN[@]}" gnatprove --version

echo "=== gnatprove img_proof.gpr"
"${RUN[@]}" gnatprove -P img_proof.gpr -j0 --level=2 --report=all \
  2>&1 | tee prove-main.log
prove_status=${PIPESTATUS[0]}
if [ "$prove_status" -ne 0 ]; then
  echo "gnatprove img_proof.gpr exited with $prove_status"; fail=1
fi
if ! grep -q 'Success: all checks proved' prove-main.log; then
  echo "gnatprove did not report that all checks were proved"; fail=1
fi
if [ ! -s obj/test-O2/gnatprove/gnatprove.out ]; then
  echo "gnatprove.out was not generated"; fail=1
fi
# GNATprove 16 は「 medium: ...」を行頭に出し、場所は次の「--> file:line:col」の行に出す
if grep -Eq '(^|: )\s*(low|medium|high|error):' prove-main.log; then
  echo "UNPROVED checks in img_proof.gpr"; fail=1
fi
if grep -q 'not proved' obj/test-O2/gnatprove/gnatprove.out 2>/dev/null; then
  echo "gnatprove.out reports unproved checks"; fail=1
fi
echo "=== gnatprove.out summary"
sed -n '/^Summary of SPARK analysis/,/^$/p' obj/test-O2/gnatprove/gnatprove.out || true

echo "=== gnatprove bug.gpr (expected to fail)"
"${RUN[@]}" gnatprove -P bug.gpr -j0 --level=2 --report=fail \
  2>&1 | tee prove-bug.log
bug_status=${PIPESTATUS[0]}
echo "gnatprove bug.gpr exit status: $bug_status"
if grep -q 'index check might fail' prove-bug.log; then
  echo "EXPECTED: index checks in bug.gpr are not proved"
else
  echo "UNEXPECTED: bug.gpr did not report an index check"; fail=1
fi

[ "$fail" -eq 0 ] && echo "ALL CHECKS PASSED"
exit $fail
