# Find-OldLogs.ps1（記事 16 章: スクリプトファイル .ps1 の基本）
# 古いログを一覧化して CSV に出力するスクリプト。
# 実行例: ./src/Find-OldLogs.ps1 -Path C:\Logs -Days 60 -OutputPath ./old-logs.csv
param(
  [string]$Path = "C:\Logs",
  [int]$Days = 30,
  [string]$OutputPath = "./old-logs.csv"
)

$limit = (Get-Date).AddDays(-$Days)

Get-ChildItem -Path $Path -Filter *.log -File -Recurse |
  Where-Object { $_.LastWriteTime -lt $limit } |
  Select-Object FullName, Length, LastWriteTime |
  Export-Csv -Path $OutputPath -NoTypeInformation -Encoding UTF8

Write-Host "Exported: $OutputPath"
