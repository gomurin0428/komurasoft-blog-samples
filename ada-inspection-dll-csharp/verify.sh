#!/usr/bin/env bash
# Linux: 共有ライブラリを作り、C から呼ぶ。.NET 10 SDK があれば C# からも呼ぶ。
# alr があれば (CI)、GNATprove で Inspect_Core を証明する。
set -euo pipefail
cd "$(dirname "$0")"

if command -v alr >/dev/null 2>&1; then
  RUN="alr -n exec --"
  alr -n build
else
  RUN=""
  gprbuild -p -P inspect.gpr
fi

$RUN gcc -Wall -o obj/test_inspect c/test_inspect.c -Llib -linspect
LD_LIBRARY_PATH=lib ./obj/test_inspect

# 読み込む前に入れたシグナルハンドラーが残るか、読めないアドレスで何が返るか
$RUN gcc -Wall -o obj/probe_signals c/probe_signals.c -ldl
./obj/probe_signals ./lib/libinspect.so
$RUN gcc -Wall -o obj/probe_bad_pointer c/probe_bad_pointer.c -Llib -linspect
set +e
LD_LIBRARY_PATH=lib ./obj/probe_bad_pointer
echo "C bad pointer probe exit code: $?"
set -e

# No_Secondary_Stack を付けても同じライブラリがビルドでき、C のテストの結果が変わらないこと
$RUN gprbuild -q -p -P inspect_nss.gpr
$RUN gcc -Wall -o obj-nss/test_inspect c/test_inspect.c -Llib-nss -linspect
LD_LIBRARY_PATH=lib-nss ./obj-nss/test_inspect > obj-nss/test_inspect.out
LD_LIBRARY_PATH=lib ./obj/test_inspect > obj/test_inspect.out
diff obj/test_inspect.out obj-nss/test_inspect.out
echo "No_Secondary_Stack: library builds and C test output is unchanged"
# 二次スタックを使う関数は、この指定でコンパイルエラーになること
if out=$($RUN gcc -c -gnatec=gnat.adc -o obj-nss/nss_violation.o nss/nss_violation.adb 2>&1); then
  echo "UNEXPECTED SUCCESS: nss/nss_violation.adb"
  exit 1
fi
echo "$out"
grep -qF 'violation of restriction "No_Secondary_Stack"' <<<"$out"

if command -v alr >/dev/null 2>&1; then
  $RUN gnatprove --version
  $RUN gnatprove -P inspect.gpr --level=2 --report=all --checks-as-errors=on -j0
  sed -n '1,40p' obj/gnatprove/gnatprove.out
else
  echo "SKIP: gnatprove (alr not found)"
fi

if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks | grep -q '^10\.'; then
  dotnet build cs/InspectConsole -c Release -o out/console -nologo
  LD_LIBRARY_PATH="$PWD/lib" dotnet out/console/InspectConsole.dll
  # 結果を記録するだけで、失敗にはしない
  for probe in probe-nre probe-badptr; do
    set +e
    LD_LIBRARY_PATH="$PWD/lib" dotnet out/console/InspectConsole.dll "$probe" 2>&1 | head -20
    echo "dotnet $probe exit code: ${PIPESTATUS[0]}"
    set -e
  done
  # WPF の例はコンパイルだけ確かめる（実行は Windows で）
  dotnet build cs/InspectWpf -c Release -o out/wpf -nologo -p:EnableWindowsTargeting=true
else
  echo "SKIP: .NET 10 SDK not found"
fi
echo "ALL CHECKS PASSED"
