#Requires -Version 7.0
<#
    記事「PowerShellコマンドの基本」 2〜4 章のコード集。
    オブジェクトとしての出力、バージョン確認、Verb-Noun の命名規則を確認します。

    実行例:
        pwsh -NoProfile -File ./src/01-get-started.ps1
#>
[CmdletBinding()]
param(
    [string] $WorkspacePath = (Join-Path ([System.IO.Path]::GetTempPath()) 'powershell-command-basics/01-get-started')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/New-SampleWorkspace.ps1"
New-SampleWorkspace -Path $WorkspacePath | Out-Null

Push-Location $WorkspacePath
try {
    # --- 記事 2 章: 出力は文字列ではなくオブジェクト ---

    Get-ChildItem

    # 7日以内に更新されたファイルだけを、更新日時の新しい順に表示する
    Get-ChildItem -File |
      Where-Object { $_.LastWriteTime -gt (Get-Date).AddDays(-7) } |
      Sort-Object LastWriteTime -Descending |
      Select-Object Name, Length, LastWriteTime

    # --- 記事 3 章: Windows PowerShell 5.1 と PowerShell 7.x ---

    # 実務では最初にバージョンを確認する
    $PSVersionTable

    # 特定バージョン以上で動かしたいスクリプトには、先頭に条件を書く
    # （このファイルの先頭でも #Requires -Version 7.0 を使っている）

    # --- 記事 4 章: コマンドレットの名前は Verb-Noun ---

    # Get-Process / Get-Service / Get-ChildItem / Copy-Item / Remove-Item / Export-Csv
    # のような「動詞-名詞」の形。実体を確認してみる
    Get-Command Get-Process, Get-ChildItem, Copy-Item, Remove-Item, Export-Csv

    # dir のような短い名前はエイリアス
    Get-Command dir

    # 読めるが、スクリプトでは避けたい
    # ls *.log

    # 意図が明確（記事は Get-ChildItem -Filter *.log。ここでは logs フォルダーを対象にする）
    Get-ChildItem ./logs -Filter *.log
}
finally {
    Pop-Location
}
