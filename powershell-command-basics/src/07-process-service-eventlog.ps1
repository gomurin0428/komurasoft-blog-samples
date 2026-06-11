#Requires -Version 7.0
<#
    記事「PowerShellコマンドの基本」 12〜13 章のコード集。
    プロセス・サービス・イベントログの調査を確認します。
    Get-Service / Restart-Service / Get-WinEvent は Windows 専用コマンドレットのため、
    $IsWindows ガードの中に置いています。

    実行例:
        pwsh -NoProfile -File ./src/07-process-service-eventlog.ps1
#>
[CmdletBinding()]
param(
    [string] $WorkspacePath = (Join-Path ([System.IO.Path]::GetTempPath()) 'powershell-command-basics/07-process')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/New-SampleWorkspace.ps1"
New-SampleWorkspace -Path $WorkspacePath | Out-Null

Push-Location $WorkspacePath
try {
    # --- 記事 12 章: プロセスを見る ---

    Get-Process

    # メモリ使用量の大きいプロセスを確認する
    Get-Process |
      Sort-Object WorkingSet -Descending |
      Select-Object -First 10 Name, Id, WorkingSet

    # 名前で絞る（notepad が動いていない環境ではエラーになるためガードしている）
    if (Get-Process -Name notepad -ErrorAction SilentlyContinue) {
        Get-Process -Name notepad

        # 停止は慎重に行う。まず -WhatIf で確認する
        Stop-Process -Name notepad -WhatIf

        # 問題なければ実行する（サンプルでは実行しない）
        # Stop-Process -Name notepad
    }

    # --- 記事 12 章: サービスを見る（Windows 専用） ---

    if ($IsWindows) {
        Get-Service

        # 停止中のサービスだけ見る
        Get-Service |
          Where-Object { $_.Status -eq "Stopped" }

        # 名前で絞る（Spooler が無い環境を考慮してガードしている）
        if (Get-Service -Name "Spooler" -ErrorAction SilentlyContinue) {
            Get-Service -Name "Spooler"

            # 再起動する場合も、対象確認を先に行う
            Restart-Service -Name "Spooler" -WhatIf
        }
    }

    # --- 記事 13 章: イベントログの基本（Windows 専用） ---

    if ($IsWindows) {
        # 最近のエラーを確認する
        Get-WinEvent -LogName System -MaxEvents 100 |
          Where-Object { $_.LevelDisplayName -eq "Error" } |
          Select-Object TimeCreated, ProviderName, Id, Message

        # アプリケーションログの直近50件を見る
        Get-WinEvent -LogName Application -MaxEvents 50 |
          Select-Object TimeCreated, ProviderName, Id, LevelDisplayName, Message

        # 期間で絞る
        $start = (Get-Date).AddHours(-24)

        Get-WinEvent -FilterHashtable @{
          LogName = "System"
          StartTime = $start
        } |
          Select-Object TimeCreated, ProviderName, Id, LevelDisplayName, Message

        # 調査結果は CSV に残しておくと、後で共有しやすい
        Get-WinEvent -LogName System -MaxEvents 500 |
          Where-Object { $_.LevelDisplayName -in @("Error", "Warning") } |
          Select-Object TimeCreated, ProviderName, Id, LevelDisplayName, Message |
          Export-Csv ./system-events.csv -NoTypeInformation -Encoding UTF8
    }
}
finally {
    Pop-Location
}
