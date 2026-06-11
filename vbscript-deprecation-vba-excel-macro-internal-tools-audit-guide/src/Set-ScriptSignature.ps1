# 署名の付与と確認（記事「置換の最小サンプル」）
#
# Windows 専用: 証明書ストア（Cert: ドライブ）とコード署名証明書が必要です。
# Linux / macOS では実行できないため、$IsWindows でガードしています。
param(
    [string] $FilePath = ".\Normalize.ps1"
)

if (-not $IsWindows) {
    Write-Warning "このスクリプトは Windows 専用です（Cert: ドライブと Authenticode 署名を使用します）。"
    return
}

$cert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert | Select-Object -First 1
Set-AuthenticodeSignature -FilePath $FilePath -Certificate $cert
Get-AuthenticodeSignature -FilePath $FilePath
