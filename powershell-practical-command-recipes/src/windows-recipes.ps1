<#
Windows 固有コマンドレットのレシピ集（記事 8・14・16・24・25・32・33 章）

Get-Service によるサービスの調査・集計・前後比較、Get-WinEvent によるイベントログ調査、
サービス一覧のクリップボード送信をまとめて実行します。

Get-Service / Get-WinEvent / Restart-Service は Windows 固有のコマンドレットのため、
このスクリプトは Windows 以外では警告を出して終了します。

変更系の Restart-Service は -WhatIf（予行）のみ実行します。
#>
[CmdletBinding()]
param(
    # 出力ファイル（CSV）を置く作業フォルダー。省略時は一時フォルダーに作る
    [string] $WorkRoot = (Join-Path ([IO.Path]::GetTempPath()) ("recipes-windows-" + [guid]::NewGuid().ToString("N")))
)

if (-not $IsWindows) {
    Write-Warning 'このスクリプトのレシピは Windows 固有のコマンドレット（Get-Service、Get-WinEvent など）を使うため、Windows でのみ実行できます。'
    return
}

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path $WorkRoot -Force | Out-Null

Push-Location $WorkRoot
try {
    # ===== 24. サービスを見る =====

    # サービスの一覧を見る
    Get-Service

    # 停止中のサービスだけを見る
    Get-Service |
      Where-Object { $_.Status -eq "Stopped" }

    # 自動起動なのに停止しているサービスを探す（33 章「自動起動なのに止まっているサービスを見る」も同形）
    Get-Service |
      Where-Object { $_.StartType -eq "Automatic" -and $_.Status -ne "Running" } |
      Select-Object Name, DisplayName, Status, StartType

    # 再起動する場合も、まず対象を確認する
    Get-Service -Name "Spooler"

    # 予行する（実際の再起動はメンテナンス時間や復旧手順を確認してから）
    Restart-Service -Name "Spooler" -WhatIf

    # ===== 8. 種類ごとに集計する ── Group-Object =====

    # サービスの状態ごとに数える
    Get-Service |
      Group-Object -Property Status -NoElement

    # イベントログのレベルごとに数える
    Get-WinEvent -LogName System -MaxEvents 500 |
      Group-Object -Property LevelDisplayName -NoElement

    # ===== 32. よく使う比較演算子（-in の例） =====

    Get-Service |
      Where-Object { $_.Status -in @("Running", "Paused") }

    # ===== 25. イベントログを見る =====

    # システムログの直近100件を見る
    Get-WinEvent -LogName System -MaxEvents 100 |
      Select-Object TimeCreated, ProviderName, Id, LevelDisplayName, Message

    # エラーと警告だけに絞る
    Get-WinEvent -LogName System -MaxEvents 500 |
      Where-Object { $_.LevelDisplayName -in @("Error", "Warning") } |
      Select-Object TimeCreated, ProviderName, Id, LevelDisplayName, Message

    # 直近24時間のエラーだけを見る
    $start = (Get-Date).AddHours(-24)

    Get-WinEvent -FilterHashtable @{ LogName = "System"; StartTime = $start } |
      Where-Object { $_.LevelDisplayName -eq "Error" } |
      Select-Object TimeCreated, ProviderName, Id, Message

    # 調査結果を CSV に残す
    Get-WinEvent -LogName System -MaxEvents 500 |
      Where-Object { $_.LevelDisplayName -in @("Error", "Warning") } |
      Select-Object TimeCreated, ProviderName, Id, LevelDisplayName, Message |
      Export-Csv .\system-events.csv -NoTypeInformation -Encoding UTF8

    # システムログのエラーを CSV にする（33 章）
    Get-WinEvent -LogName System -MaxEvents 500 |
      Where-Object { $_.LevelDisplayName -eq "Error" } |
      Select-Object TimeCreated, ProviderName, Id, Message |
      Export-Csv .\system-errors.csv -NoTypeInformation -Encoding UTF8

    # ===== 14. 2つの結果を比較する ── Compare-Object（サービス一覧の前後比較） =====
    # 本来は「変更前に出す → 変更する → 変更後に出す」という流れ。
    # このサンプルでは同じ状態を2回出すため、差分は出ない

    Get-Service |
      Select-Object Name, Status |
      Export-Csv .\services-before.csv -NoTypeInformation -Encoding UTF8

    Get-Service |
      Select-Object Name, Status |
      Export-Csv .\services-after.csv -NoTypeInformation -Encoding UTF8

    Compare-Object `
      -ReferenceObject (Import-Csv .\services-before.csv) `
      -DifferenceObject (Import-Csv .\services-after.csv) `
      -Property Name, Status

    # ===== 16. クリップボードへ送る ── Set-Clipboard =====

    Get-Service |
      Select-Object Name, Status |
      ConvertTo-Csv -NoTypeInformation |
      Set-Clipboard

    # コマンド結果をクリップボードへ送る（33 章）
    Get-Service |
      Select-Object Name, DisplayName, Status |
      ConvertTo-Csv -NoTypeInformation |
      Set-Clipboard
}
finally {
    Pop-Location
}
