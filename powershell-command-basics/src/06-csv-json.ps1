#Requires -Version 7.0
<#
    記事「PowerShellコマンドの基本」 10〜11 章のコード集。
    CSV（Import-Csv / Export-Csv）と JSON（ConvertFrom-Json / ConvertTo-Json）を確認します。

    実行例:
        pwsh -NoProfile -File ./src/06-csv-json.ps1
#>
[CmdletBinding()]
param(
    [string] $WorkspacePath = (Join-Path ([System.IO.Path]::GetTempPath()) 'powershell-command-basics/06-csv-json')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/New-SampleWorkspace.ps1"
New-SampleWorkspace -Path $WorkspacePath | Out-Null

Push-Location $WorkspacePath
try {
    # --- 記事 10 章: CSV を扱う基本 ---

    # users.csv はワークスペース作成時に記事と同じ内容で用意している
    #   Name,Department,Enabled
    #   Suzuki,Sales,true
    #   Tanaka,Accounting,false
    #   Sato,Sales,true

    # CSV を読み込む。列名がプロパティになる
    $users = Import-Csv ./users.csv
    $users

    $users | Select-Object Name, Department

    # 条件で絞る
    $users |
      Where-Object { $_.Department -eq "Sales" }

    # CSV の値は文字列として入ることが多いため、true / false や数値は文字列として比較する
    $users |
      Where-Object { $_.Enabled -eq "true" }

    # CSV に出力する
    $users |
      Where-Object { $_.Department -eq "Sales" } |
      Export-Csv ./sales-users.csv -NoTypeInformation -Encoding UTF8

    # Export-Csv の前に Format-Table を入れないことが重要

    # 避ける
    # $users | Format-Table | Export-Csv ./out.csv -NoTypeInformation

    # 正しい
    $users | Select-Object Name, Department, Enabled | Export-Csv ./out.csv -NoTypeInformation -Encoding UTF8

    # --- 記事 11 章: JSON を扱う基本 ---

    $data = Get-Content ./settings.json -Raw | ConvertFrom-Json
    $data

    # オブジェクトを JSON に変換する
    [pscustomobject]@{
      Name = "BatchJob"
      Enabled = $true
      Retry = 3
    } | ConvertTo-Json

    # 深い階層を含む場合は -Depth を指定する
    $config = $data
    $config | ConvertTo-Json -Depth 10 | Set-Content ./settings.json -Encoding UTF8
}
finally {
    Pop-Location
}
