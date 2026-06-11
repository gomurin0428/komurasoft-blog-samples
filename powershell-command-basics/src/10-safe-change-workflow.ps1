#Requires -Version 7.0
<#
    記事「PowerShellコマンドの基本」 19・22 章のコード集。
    変更系コマンドを安全に実行する順番
    （見る → 絞る → 記録する → 予行する → 実行する）を確認します。
    記事の C:\Temp は ./temp に、C:\Work\Reports は ./reports に調整しています。

    実行例:
        pwsh -NoProfile -File ./src/10-safe-change-workflow.ps1
#>
[CmdletBinding()]
param(
    [string] $WorkspacePath = (Join-Path ([System.IO.Path]::GetTempPath()) 'powershell-command-basics/10-safe-change')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/New-SampleWorkspace.ps1"
New-SampleWorkspace -Path $WorkspacePath | Out-Null

Push-Location $WorkspacePath
try {
    # --- 記事 19 章: 30日より古い .tmp ファイルを削除する ---

    # Step 1: 対象を見る
    Get-ChildItem ./temp -Filter *.tmp -File -Recurse

    # Step 2: 条件で絞る
    $limit = (Get-Date).AddDays(-30)

    Get-ChildItem ./temp -Filter *.tmp -File -Recurse |
      Where-Object { $_.LastWriteTime -lt $limit }

    # Step 3: 必要な列だけ表示する
    $targets = Get-ChildItem ./temp -Filter *.tmp -File -Recurse |
      Where-Object { $_.LastWriteTime -lt $limit }

    $targets |
      Select-Object FullName, Length, LastWriteTime

    # Step 4: 証跡を残す
    $targets |
      Select-Object FullName, Length, LastWriteTime |
      Export-Csv ./delete-targets.csv -NoTypeInformation -Encoding UTF8

    # Step 5: WhatIf で確認する
    $targets | Remove-Item -WhatIf

    # Step 6: 実行する
    $targets | Remove-Item

    # --- 記事 22 章: 古いファイルをアーカイブする（削除ではなく移動にする例） ---

    # 対象確認
    $source = "./reports"
    $dest = "./reports-archive"
    $limit = (Get-Date).AddDays(-90)

    $targets = Get-ChildItem $source -Filter *.xlsx -File |
      Where-Object { $_.LastWriteTime -lt $limit }

    $targets | Select-Object FullName, Length, LastWriteTime

    # アーカイブ先作成
    if (-not (Test-Path $dest)) {
      New-Item -ItemType Directory -Path $dest
    }

    # 証跡出力
    $targets |
      Select-Object FullName, Length, LastWriteTime |
      Export-Csv ./archive-targets.csv -NoTypeInformation -Encoding UTF8

    # WhatIf 確認
    $targets | Move-Item -Destination $dest -WhatIf

    # 実行
    $targets | Move-Item -Destination $dest

    # ファイル名重複があり得る場合は、このままだと失敗する。
    # 実務では、年月フォルダーを分ける、移動先ファイル名に日時を付ける、
    # 既存ファイルがあればスキップする、といったルールを先に決める。
}
finally {
    Pop-Location
}
