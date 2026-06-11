#Requires -Version 7.0
<#
    記事「PowerShellコマンドの基本」 8 章のコード集。
    ファイル・フォルダー操作（場所の確認、一覧、作成、コピー、移動、削除）を確認します。
    記事の C:\Logs などの例は、ワークスペース内の logs フォルダーに調整しています。

    実行例:
        pwsh -NoProfile -File ./src/04-file-folder-operations.ps1
#>
[CmdletBinding()]
param(
    [string] $WorkspacePath = (Join-Path ([System.IO.Path]::GetTempPath()) 'powershell-command-basics/04-file-folder')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/New-SampleWorkspace.ps1"
New-SampleWorkspace -Path $WorkspacePath | Out-Null

Push-Location $WorkspacePath
try {
    # --- 現在の場所を確認する ---

    Get-Location

    # 移動する（記事は Set-Location C:\Work。ここではワークスペース内で移動する）
    Set-Location ./logs
    Get-Location
    Set-Location ..

    # パスに空白が含まれる場合は引用符で囲む
    # Set-Location "C:\Work Files\Reports"

    # --- ファイル一覧を見る ---

    Get-ChildItem
    Get-ChildItem -File
    Get-ChildItem -Directory
    Get-ChildItem -Recurse

    # 拡張子で絞る場合は -Filter が便利
    Get-ChildItem ./logs -Filter *.log

    # サブフォルダーも含めて検索する場合は -Recurse を使う
    # （大量のファイルがある場所では、対象フォルダーを限定してから実行する）
    Get-ChildItem ./logs -Filter *.log -Recurse

    # --- 作成する ---

    # フォルダー作成
    New-Item -ItemType Directory -Path ./archive

    # 空ファイル作成
    New-Item -ItemType File -Path ./memo.txt

    # 既に存在する可能性がある場合は、先に確認する
    if (-not (Test-Path ./archive)) {
      New-Item -ItemType Directory -Path ./archive
    }

    # --- コピーする ---

    Copy-Item ./users.csv ./archive/users.csv

    # フォルダーごとコピーする場合は -Recurse を使う
    # 上書きが絡む場合は、事前に影響を確認する
    Copy-Item ./logs ./archive/logs -Recurse -WhatIf
    Copy-Item ./logs ./archive/logs -Recurse

    # --- 移動する ---

    # 複数ファイルを条件で移動する場合は、最初に -WhatIf を付ける
    Get-ChildItem ./logs -Filter *.log |
      Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
      Move-Item -Destination ./archive -WhatIf

    # 問題なければ -WhatIf を外す
    Get-ChildItem ./logs -Filter *.log |
      Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
      Move-Item -Destination ./archive

    # --- 削除する ---

    # 削除は特に慎重に扱う。まず -WhatIf で削除予定を確認する
    Remove-Item ./memo.txt -WhatIf

    # 対象が正しいことを確認してから実行する
    Remove-Item ./memo.txt

    # ワイルドカードと -Recurse の組み合わせは強力。
    # 最初から本番フォルダーで実行せず、一覧表示で確認する

    # まず対象を確認
    Get-ChildItem ./logs -Filter *.tmp -Recurse |
      Select-Object FullName, Length, LastWriteTime

    # 次に削除予定を確認
    Get-ChildItem ./logs -Filter *.tmp -Recurse |
      Remove-Item -WhatIf

    # 最後に実行
    Get-ChildItem ./logs -Filter *.tmp -Recurse |
      Remove-Item
}
finally {
    Pop-Location
}
