# COM host と TLB の登録（記事 7 章）
#
# 管理者権限の PowerShell で実行してください。
#
# - regsvr32 で *.comhost.dll を COM サーバーとして登録する
# - dscom tlbregister で *.tlb をタイプライブラリとして登録する
# - 64bit Office: System32 の regsvr32 + dscom（記事 7.1）
# - 32bit Office（64bit Windows 上）: SysWOW64 の regsvr32 + dscom32.exe（記事 7.2）
#
# Windows 専用: regsvr32 とレジストリ登録を使うため、Linux / macOS では実行できません。
[CmdletBinding()]
param(
    [ValidateSet('x64', 'x86')]
    [string] $Bitness = 'x64',

    [string] $Configuration = 'Release'
)

if (-not $IsWindows) {
    Write-Warning "このスクリプトは Windows 専用です（regsvr32 と COM 登録を使用します）。"
    return
}

$out = Resolve-Path (Join-Path $PSScriptRoot '..' 'src' 'VbaTypedComSample' 'bin' $Configuration 'net8.0-windows')

if ($Bitness -eq 'x86') {
    # 32bit Office（64bit Windows 上）の場合（記事 7.2）
    & C:\Windows\SysWOW64\regsvr32.exe "$out\VbaTypedComSample.comhost.dll"
    & (Join-Path $PSScriptRoot '..' 'tools' 'dscom32.exe') tlbregister "$out\VbaTypedComSample.tlb"
}
else {
    # 64bit Office / 64bit COM の場合（記事 7.1）
    & C:\Windows\System32\regsvr32.exe "$out\VbaTypedComSample.comhost.dll"
    dscom tlbregister "$out\VbaTypedComSample.tlb"
}
