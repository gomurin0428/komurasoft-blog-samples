#Requires -Version 7.0
<#
    記事「PowerShellコマンドの基本」 5 章のコード集。
    まず覚える3つの調査コマンド（Get-Command / Get-Help / Get-Member）を確認します。

    実行例:
        pwsh -NoProfile -File ./src/02-discovery-commands.ps1
#>
[CmdletBinding()]
param(
    [string] $WorkspacePath = (Join-Path ([System.IO.Path]::GetTempPath()) 'powershell-command-basics/02-discovery')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/New-SampleWorkspace.ps1"
New-SampleWorkspace -Path $WorkspacePath | Out-Null

Push-Location $WorkspacePath
try {
    # --- 1. Get-Command ── 使えるコマンドを探す ---

    # Process という名詞を持つコマンドを探す
    Get-Command -Noun Process

    # Service 関連のコマンドを探す
    Get-Command *Service*

    # CSV 関連のコマンドを探す
    Get-Command *Csv*

    # コマンド名がうろ覚えのときは、ワイルドカードを使う
    Get-Command *Item*
    Get-Command *Content*
    Get-Command *Json*

    # --- 2. Get-Help ── 使い方を見る ---

    Get-Help Get-ChildItem
    Get-Help Get-ChildItem -Examples
    # Get-Help Get-ChildItem -Full     # 出力が長いためコメントにしている
    # Get-Help Get-ChildItem -Online   # 既定のブラウザーを開くためコメントにしている

    # 最初は -Examples が便利
    Get-Help Where-Object -Examples

    # ヘルプが古い・不足している場合は、管理者権限の PowerShell で更新する
    # （ネットワーク接続と権限が必要なためコメントにしている）
    # Update-Help

    # --- 3. Get-Member ── オブジェクトの中身を見る ---

    Get-Process | Get-Member
    Get-ChildItem | Get-Member

    # Get-Service は Windows 専用コマンドレット
    if ($IsWindows) {
        Get-Service | Get-Member

        # サービスには Status や Name などのプロパティがある
        Get-Service | Select-Object Name, Status
    }

    # ファイルには Name、Length、LastWriteTime などがある
    Get-ChildItem -File | Select-Object Name, Length, LastWriteTime
}
finally {
    Pop-Location
}
