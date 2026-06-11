# Register-LogMaintenanceTask.ps1
# 記事 11 章「タスクスケジューラで定期実行する」のスクリプトです。
# Windows のタスクスケジューラ（ScheduledTasks モジュール）専用のため、
# Windows 以外では実行できません。パスは環境に合わせて変更してください。
#Requires -Version 7.0

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $IsWindows) {
  throw "This script registers a Windows Task Scheduler task and runs on Windows only."
}

$scriptPath = "C:\Ops\Invoke-LogMaintenance.ps1"
$configPath = "C:\Ops\log-maintenance.json"

$action = New-ScheduledTaskAction `
  -Execute "pwsh.exe" `
  -Argument "-NoProfile -File `"$scriptPath`" -ConfigPath `"$configPath`"" `
  -WorkingDirectory "C:\Ops"

$trigger = New-ScheduledTaskTrigger -Daily -At 3:00

Register-ScheduledTask `
  -TaskName "AppLogMaintenance" `
  -Action $action `
  -Trigger $trigger `
  -Description "Collect app log errors and archive old logs"
