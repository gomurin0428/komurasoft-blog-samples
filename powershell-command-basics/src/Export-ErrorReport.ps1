# Export-ErrorReport.ps1（記事 21 章: 実務サンプル ── ログを調査してレポートを作る）
# 直近 N 日以内に更新された .log ファイルから、パターンに一致する行を CSV にまとめる。
# 実行例: ./src/Export-ErrorReport.ps1 -LogPath C:\App\Logs -Days 14 -Pattern "ERROR|FATAL" -OutputPath ./errors.csv
param(
  [string]$LogPath = "C:\App\Logs",
  [int]$Days = 7,
  [string]$Pattern = "ERROR",
  [string]$OutputPath = "./error-report.csv"
)

$since = (Get-Date).AddDays(-$Days)

Get-ChildItem $LogPath -Filter *.log -File -Recurse |
  Where-Object { $_.LastWriteTime -ge $since } |
  Select-String -Pattern $Pattern |
  Select-Object Path, LineNumber, Line |
  Export-Csv $OutputPath -NoTypeInformation -Encoding UTF8

Write-Host "Exported: $OutputPath"
