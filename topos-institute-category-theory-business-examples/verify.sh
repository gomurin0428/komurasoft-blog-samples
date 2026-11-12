#!/usr/bin/env bash
# 記事「圏論は業務システムで何に使えるか」の Julia の例を実行し、
# 出力が expected/ の内容（記事に載せた出力）と一致することを確かめる。
set -euo pipefail
cd "$(dirname "$0")"

JULIA=${JULIA:-julia}
"$JULIA" --version
"$JULIA" --project=. -e 'using Pkg; Pkg.instantiate(); Pkg.status()'

fail=0
check() {
  local script=$1 expected=$2 actual
  echo "=== $script"
  actual=$(mktemp)
  "$JULIA" --project=. "$script" | tee "$actual"
  if diff -u "$expected" "$actual"; then
    echo "OK: $script"
  else
    echo "MISMATCH: $script"
    fail=1
  fi
  rm -f "$actual"
}

check run_orders.jl expected/run_orders.out
check 04_compose_graph.jl expected/04_compose_graph.out

exit $fail
