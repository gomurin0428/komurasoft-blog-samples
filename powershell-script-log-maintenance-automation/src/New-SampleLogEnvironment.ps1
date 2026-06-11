# New-SampleLogEnvironment.ps1
# 記事のフォルダー構成（Logs / Reports / Archive）を一時ディレクトリ上に再現し、
# ダミーログと設定ファイル log-maintenance.json を作る補助スクリプトです。
# 本番フォルダーに触れずに Invoke-LogMaintenance.ps1 を安全に試せます。
#Requires -Version 7.0

[CmdletBinding()]
param(
  [ValidateNotNullOrEmpty()]
  [string]$Root = (Join-Path ([System.IO.Path]::GetTempPath()) ("log-maintenance-sample-" + (Get-Date -Format "yyyyMMdd-HHmmss")))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$logPath = Join-Path $Root "Logs"
$outputPath = Join-Path $Root "Reports"
$archivePath = Join-Path $Root "Archive"

foreach ($path in @($logPath, (Join-Path $logPath "old"), $outputPath, $archivePath)) {
  New-Item -ItemType Directory -Path $path -Force | Out-Null
}

# 直近のログ（調査対象。ERROR / WARN / FATAL を含む）
$now = Get-Date

@(
  "{0:yyyy-MM-dd HH:mm:ss} INFO  application started" -f $now
  "{0:yyyy-MM-dd HH:mm:ss} ERROR failed to connect to database" -f $now
  "{0:yyyy-MM-dd HH:mm:ss} WARN  disk usage 85 percent" -f $now
  "{0:yyyy-MM-dd HH:mm:ss} INFO  application stopped" -f $now
) | Set-Content -LiteralPath (Join-Path $logPath "app.log") -Encoding UTF8

@(
  "{0:yyyy-MM-dd HH:mm:ss} INFO  batch started" -f $now
  "{0:yyyy-MM-dd HH:mm:ss} FATAL batch aborted" -f $now
) | Set-Content -LiteralPath (Join-Path $logPath "batch.log") -Encoding UTF8

# 古いログ（アーカイブ対象。LastWriteTime を 120 日前に設定）
$oldLog = Join-Path $logPath "old/app-202401.log"
@(
  "2024-01-15 03:00:00 INFO  old log entry"
  "2024-01-15 03:00:01 ERROR old error entry"
) | Set-Content -LiteralPath $oldLog -Encoding UTF8
(Get-Item -LiteralPath $oldLog).LastWriteTime = $now.AddDays(-120)

# 設定ファイル
$configPath = Join-Path $Root "log-maintenance.json"
[ordered]@{
  LogPath     = $logPath
  OutputPath  = $outputPath
  Days        = 7
  Patterns    = @("ERROR", "WARN", "FATAL")
  ArchiveDays = 90
  ArchivePath = $archivePath
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $configPath -Encoding UTF8

Write-Host "Sample environment created: $Root"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  pwsh -NoProfile -File ./src/Invoke-LogMaintenance.ps1 -ConfigPath `"$configPath`" -SkipArchive"
Write-Host "  pwsh -NoProfile -File ./src/Invoke-LogMaintenance.ps1 -ConfigPath `"$configPath`" -Preview"
Write-Host "  pwsh -NoProfile -File ./src/Invoke-LogMaintenance.ps1 -ConfigPath `"$configPath`""

[pscustomobject]@{
  Root       = $Root
  ConfigPath = $configPath
  LogPath    = $logPath
  OutputPath = $outputPath
  ArchivePath = $archivePath
}
