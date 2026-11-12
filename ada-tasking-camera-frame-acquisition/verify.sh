#!/usr/bin/env bash
# 記事「取りこぼさない取り込み処理を Ada のタスクで作る」のコードを確かめる。
# 1. ビルドと実行（処理が周期に収まる場合、収まらない場合、待つ方針の場合）
# 2. GNATprove ですべての検査が証明されること
# 3. わざと誤りを入れた2つの版で、GNATprove がデータ競合と優先度上限の違反を報告すること
# CI では alr が環境（GNAT・gprbuild・GNATprove）を用意する。
# alr が無い環境では PATH 上の gprbuild を使い、GNATprove の段階は飛ばす。
set -euo pipefail
cd "$(dirname "$0")"

if command -v alr >/dev/null 2>&1; then
  RUN=(alr -n exec --)
  alr -n build
else
  RUN=()
  gprbuild -q -p -P frame_capture.gpr
fi

"${RUN[@]}" gcc --version | head -1
B=./bin/capture_main
echo "--- run: work 5 ms"
"$B" frames=300 period_ms=10 work_ms=5
echo "--- run: work 12 ms"
"$B" frames=300 period_ms=10 work_ms=12
echo "--- run: work 12 ms, wait policy"
"$B" frames=300 period_ms=10 work_ms=12 policy=wait

if [ ${#RUN[@]} -eq 0 ]; then
  echo "SKIP gnatprove (alr not found)"
  echo "ALL CHECKS PASSED (without gnatprove)"
  exit 0
fi

echo "--- gnatprove"
"${RUN[@]}" gnatprove --version
"${RUN[@]}" gnatprove -P frame_capture.gpr --level=2 --report=all -j0 \
  --checks-as-errors=on 2>&1 | tee prove.log
grep -q 'Success: all checks proved' prove.log
sed -n '/Summary of SPARK analysis/,/^$/p' obj/gnatprove/gnatprove.out

# 誤りを入れた版を作り、GNATprove が決まったメッセージを報告することを確かめる
fail=0
negative() {
  local name="$1" file="$2" from="$3" to="$4" pattern="$5"
  rm -rf "neg-$name"
  mkdir "neg-$name"
  cp -r src gnat.adc frame_capture.gpr "neg-$name/"
  sed -i "s/$from/$to/" "neg-$name/src/$file"
  if ! grep -q -- "$to" "neg-$name/src/$file"; then
    echo "NEGATIVE $name: substitution failed"
    fail=1
    return
  fi
  echo "--- negative: $name"
  local out
  out=$("${RUN[@]}" gnatprove -P "neg-$name/frame_capture.gpr" --level=2 -j0 2>&1 || true)
  echo "$out" | grep -iE 'race|ceiling|error|high|medium' | head -20 || true
  if grep -qF -- "$pattern" <<<"$out"; then
    echo "NEGATIVE $name: detected as expected"
  else
    echo "NEGATIVE $name: NOT detected"
    fail=1
  fi
}

# 反例1: 画像の領域から Synchronous を外すと、2つのタスクが同じ状態に触るのでデータ競合になる
negative race frame_pool.ads "Pixels with Synchronous, External" "Pixels with External" \
  'possible data race when accessing variable "frame_pool.pixels"'
# 反例2: Slot_Queue の優先度上限を処理タスクと同じ10に下げる
negative ceiling slot_queues.ads "with Priority => Capture_Priorities.Camera_Priority" \
  "with Priority => Capture_Priorities.Worker_Priority" \
  'ceiling priority protocol might not be respected'

[ "$fail" -eq 0 ] && echo "ALL CHECKS PASSED"
exit $fail
