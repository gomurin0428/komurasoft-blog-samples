# ビルドと dscom による TLB 生成（記事 5 章・6 章）
#
# - 64bit Office 向け（既定）: dscom tlbexport を使う（記事 6.1）
# - 32bit Office 向け: dscom32.exe を使う（記事 6.2）。
#   あらかじめ tools\dscom32.exe を配置しておいてください。
#
# Windows 専用: TLB の生成は Windows のタイプライブラリ API を使うため、
# Linux / macOS では実行できません。$IsWindows でガードしています。
[CmdletBinding()]
param(
    [ValidateSet('x64', 'x86')]
    [string] $Bitness = 'x64',

    [string] $Configuration = 'Release'
)

if (-not $IsWindows) {
    Write-Warning "このスクリプトは Windows 専用です（dscom tlbexport は Windows のタイプライブラリ API を使用します）。"
    return
}

$projectDir = Join-Path $PSScriptRoot '..' 'src' 'VbaTypedComSample'

# Release ビルド（記事 5 章）。32bit Office なら x64 を x86、win-x64 を win-x86 に読み替える（記事 3 章）
if ($Bitness -eq 'x86') {
    dotnet build $projectDir -c $Configuration -p:PlatformTarget=x86 -p:NETCoreSdkRuntimeIdentifier=win-x86
}
else {
    dotnet build $projectDir -c $Configuration
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "ビルドに失敗しました。"
    return
}

$outDir = Join-Path $projectDir 'bin' $Configuration 'net8.0-windows'
$dll = Join-Path $outDir 'VbaTypedComSample.dll'
$tlb = Join-Path $outDir 'VbaTypedComSample.tlb'

if ($Bitness -eq 'x86') {
    # 32bit Office 向けに TLB を作るなら dscom32.exe を使う（記事 6.2）
    & (Join-Path $PSScriptRoot '..' 'tools' 'dscom32.exe') tlbexport $dll --out $tlb
}
else {
    dscom tlbexport $dll --out $tlb
}
