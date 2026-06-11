# Scheduled Task から VBScript 呼び出しを抽出（記事「ファイル検索と構成情報の洗い出し」）
#
# Windows 専用: Get-ScheduledTask は Windows の ScheduledTasks モジュールを使用します。
# Linux / macOS では実行できないため、$IsWindows でガードしています。
param(
    [string] $OutputCsv = ".\task-vbscript-dependencies.csv"
)

if (-not $IsWindows) {
    Write-Warning "このスクリプトは Windows 専用です（Get-ScheduledTask を使用します）。"
    return
}

Get-ScheduledTask | ForEach-Object {
  foreach ($a in $_.Actions) {
    if ($a.Execute -match 'wscript|cscript|mshta' -or $a.Arguments -match '\.vbs\b') {
      [pscustomobject]@{
        TaskName  = $_.TaskName
        TaskPath  = $_.TaskPath
        Execute   = $a.Execute
        Arguments = $a.Arguments
      }
    }
  }
} | Export-Csv $OutputCsv -NoTypeInformation -Encoding UTF8
