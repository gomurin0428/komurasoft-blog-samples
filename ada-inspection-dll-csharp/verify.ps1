# Windows: Ada の DLL を作り、.NET 10 のコンソールアプリから呼ぶ。WPF の例はビルドだけ。
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

function Invoke-Checked([string]$What, [scriptblock]$Block) {
    & $Block
    if ($LASTEXITCODE -ne 0) { throw "$What failed (exit code $LASTEXITCODE)" }
}

Invoke-Checked 'alr build' { alr -n build }
Get-ChildItem lib -Filter *.dll | Format-Table Name, Length

# GNAT のランタイムの DLL は、Alire のツールチェーンの gcc と同じ bin にある
function Find-GnatBin {
    # 1) Alire のツールチェーンのキャッシュ
    foreach ($root in @("$env:LOCALAPPDATA\alire\cache\toolchains",
                        "$env:USERPROFILE\.cache\alire\toolchains")) {
        if (Test-Path $root) {
            $dir = Get-ChildItem $root -Directory -Filter 'gnat_native_*' |
                Sort-Object Name -Descending |
                Where-Object { Test-Path (Join-Path $_.FullName 'bin\gcc.exe') } |
                Select-Object -First 1
            if ($dir) { return (Join-Path $dir.FullName 'bin') }
        }
    }
    # 2) alr exec の環境で where.exe に探させる
    $found = @(alr -n exec -- where.exe gcc 2>$null) |
        Where-Object { $_ -match 'gcc\.exe\s*$' } | Select-Object -First 1
    if ($found) { return (Split-Path $found.Trim()) }
    # 3) Alire のフォルダーの下を探す
    $g = Get-ChildItem "$env:LOCALAPPDATA\alire" -Recurse -Filter gcc.exe -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($g) { return $g.DirectoryName }
    return $null
}

$gnatBin = Find-GnatBin
if (-not $gnatBin) { throw 'GNAT toolchain bin not found' }
"gnat bin: $gnatBin"
& (Join-Path $gnatBin 'gcc.exe') --version | Select-Object -First 1
Get-ChildItem $gnatBin -Filter *.dll | ForEach-Object { "toolchain dll: $($_.Name)" }

# DLL が依存する DLL の一覧（objdump が無ければランタイムの DLL の名前で代える）
$objdump = Join-Path $gnatBin 'objdump.exe'
if (Test-Path $objdump) {
    $deps = @(& $objdump -p lib\libinspect.dll |
        Select-String 'DLL Name' |
        ForEach-Object { ($_.Line -split ':', 2)[1].Trim() })
} else {
    'objdump.exe not found; using libgnat*/libgcc_s* from the toolchain'
    $deps = @(Get-ChildItem $gnatBin -Include 'libgnat*.dll', 'libgcc_s*.dll' -Recurse -Depth 0 |
        ForEach-Object { $_.Name })
}
$deps | ForEach-Object { "depends on: $_" }

dotnet --list-sdks
"dotnet --version: $(dotnet --version)"
if (-not (dotnet --list-sdks | Select-String '^10\.')) { throw '.NET 10 SDK is required (actions/setup-dotnet)' }

Invoke-Checked 'build console' { dotnet build cs\InspectConsole -c Release -o out\console -nologo }
Copy-Item lib\libinspect.dll out\console
$copied = @()
$queue = [System.Collections.Generic.Queue[string]]::new()
$deps | ForEach-Object { $queue.Enqueue($_) }
while ($queue.Count -gt 0) {
    $d = $queue.Dequeue()
    $src = Join-Path $gnatBin $d
    if ((Test-Path $src) -and ($copied -notcontains $d)) {
        Copy-Item $src out\console
        $copied += $d
        # ランタイムの DLL がさらに依存するツールチェーンの DLL も置く
        if (Test-Path $objdump) {
            & $objdump -p $src | Select-String 'DLL Name' |
                ForEach-Object { $queue.Enqueue(($_.Line -split ':', 2)[1].Trim()) }
        }
    }
}
"copied runtime DLLs: $($copied -join ', ')"
Invoke-Checked 'run console' { & out\console\InspectConsole.exe }

# 追加の確かめ。結果を記録するだけで、失敗にはしない
foreach ($probe in 'probe-nre', 'probe-badptr') {
    $ErrorActionPreference = 'Continue'
    $output = @(& out\console\InspectConsole.exe $probe 2>&1 | ForEach-Object { "$_" })
    $code = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    $output | Select-Object -First 20
    "$probe exit code: $code"
}

# GNAT のランタイムの DLL を置かなかった場合
if ($copied.Count -gt 0) {
    Copy-Item out\console out\console-missing -Recurse
    foreach ($d in $copied) { Remove-Item (Join-Path out\console-missing $d) }
    # 出力を先に変数へ受けてから終了コードを読む（Select-Object -First でパイプを
    # 途中で切ると、$LASTEXITCODE が実行ファイルの値にならない）
    $ErrorActionPreference = 'Continue'
    $output = @(& out\console-missing\InspectConsole.exe 2>&1 | ForEach-Object { "$_" })
    $code = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    $output | Select-Object -First 3
    "exit code without runtime DLLs: $code"
    if ($code -eq 0) { throw 'expected a failure without the runtime DLLs' }
    if (-not ($output -match 'DllNotFoundException')) { throw 'expected DllNotFoundException in the output' }
} else {
    'no GNAT runtime DLL dependency found'
}

Invoke-Checked 'build wpf' { dotnet build cs\InspectWpf -c Release -nologo }
'ALL WINDOWS CHECKS PASSED'
