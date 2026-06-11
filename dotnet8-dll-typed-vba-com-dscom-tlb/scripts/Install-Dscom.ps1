# dscom（タイプライブラリ生成ツール）のインストール（記事 6.1）
#
# Windows 専用: dscom は Windows のタイプライブラリ API を使うため、
# Linux / macOS では実行できません。$IsWindows でガードしています。
[CmdletBinding()]
param()

if (-not $IsWindows) {
    Write-Warning "このスクリプトは Windows 専用です（dscom は Windows のタイプライブラリ API を使用します）。"
    return
}

dotnet tool install --global dscom
