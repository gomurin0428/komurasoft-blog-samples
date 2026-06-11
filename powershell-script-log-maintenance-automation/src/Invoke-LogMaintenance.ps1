# Invoke-LogMaintenance.ps1
#Requires -Version 7.0

[CmdletBinding()]
param(
  [ValidateNotNullOrEmpty()]
  [string]$ConfigPath = ".\log-maintenance.json",

  [switch]$Preview,

  [switch]$SkipArchive,

  [switch]$SkipTranscript
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Ensure-Directory {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)]
    [string]$Path
  )

  if (-not [System.IO.Directory]::Exists($Path)) {
    [System.IO.Directory]::CreateDirectory($Path) | Out-Null
  }
}

function Import-LogMaintenanceConfig {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)]
    [string]$Path
  )

  if (-not (Test-Path -LiteralPath $Path)) {
    throw "Config file not found: $Path"
  }

  $config = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json

  foreach ($name in @("LogPath", "OutputPath", "Days", "Patterns", "ArchiveDays", "ArchivePath")) {
    if (-not ($config.PSObject.Properties.Name -contains $name)) {
      throw "Config value missing: $name"
    }
  }

  if ([string]::IsNullOrWhiteSpace([string]$config.LogPath)) {
    throw "LogPath is empty."
  }

  if (-not (Test-Path -LiteralPath $config.LogPath)) {
    throw "LogPath not found: $($config.LogPath)"
  }

  if ([string]::IsNullOrWhiteSpace([string]$config.OutputPath)) {
    throw "OutputPath is empty."
  }

  if ([string]::IsNullOrWhiteSpace([string]$config.ArchivePath)) {
    throw "ArchivePath is empty."
  }

  if (@($config.Patterns).Count -eq 0) {
    throw "Patterns is empty."
  }

  if ([int]$config.Days -lt 1) {
    throw "Days must be 1 or greater."
  }

  if ([int]$config.ArchiveDays -lt 1) {
    throw "ArchiveDays must be 1 or greater."
  }

  return $config
}

function Export-CsvWithHeader {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)]
    [AllowEmptyCollection()]
    [object[]]$InputObject,

    [Parameter(Mandatory)]
    [string]$Path,

    [Parameter(Mandatory)]
    [string[]]$Header
  )

  if ($InputObject.Count -gt 0) {
    $InputObject |
      Export-Csv -LiteralPath $Path -NoTypeInformation -Encoding UTF8
  }
  else {
    ($Header -join ",") |
      Set-Content -LiteralPath $Path -Encoding UTF8
  }
}

function Get-LogHit {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)]
    [string]$LogPath,

    [Parameter(Mandatory)]
    [ValidateRange(1, 3650)]
    [int]$Days,

    [Parameter(Mandatory)]
    [string[]]$Pattern
  )

  $since = (Get-Date).AddDays(-$Days)

  $files = @(
    Get-ChildItem -LiteralPath $LogPath -Filter *.log -File -Recurse -ErrorAction Stop |
      Where-Object { $_.LastWriteTime -ge $since }
  )

  Write-Verbose "Recent log files: $($files.Count)"

  if ($files.Count -eq 0) {
    return @()
  }

  $paths = $files | Select-Object -ExpandProperty FullName

  Select-String -LiteralPath $paths -Pattern $Pattern -ErrorAction Stop |
    ForEach-Object {
      [pscustomobject]@{
        Path       = $_.Path
        LineNumber = $_.LineNumber
        Pattern    = $_.Pattern
        Line       = $_.Line.Trim()
      }
    }
}

function Get-OldLogFile {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)]
    [string]$LogPath,

    [Parameter(Mandatory)]
    [ValidateRange(1, 3650)]
    [int]$ArchiveDays
  )

  $limit = (Get-Date).AddDays(-$ArchiveDays)

  Get-ChildItem -LiteralPath $LogPath -Filter *.log -File -Recurse -ErrorAction Stop |
    Where-Object { $_.LastWriteTime -lt $limit } |
    Sort-Object LastWriteTime
}

function Move-OldLogFile {
  [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = "Medium")]
  param(
    [Parameter(Mandatory)]
    [System.IO.FileInfo[]]$File,

    [Parameter(Mandatory)]
    [string]$SourceRoot,

    [Parameter(Mandatory)]
    [string]$ArchiveRoot
  )

  foreach ($item in $File) {
    $relativePath = [System.IO.Path]::GetRelativePath($SourceRoot, $item.FullName)
    $destination = Join-Path $ArchiveRoot $relativePath
    $destinationDirectory = Split-Path -Path $destination -Parent

    if (Test-Path -LiteralPath $destination) {
      $name = [System.IO.Path]::GetFileNameWithoutExtension($item.Name)
      $ext = $item.Extension
      $destination = Join-Path $destinationDirectory ("{0}_{1:yyyyMMddHHmmss}{2}" -f $name, $item.LastWriteTime, $ext)
    }

    if ($PSCmdlet.ShouldProcess($item.FullName, "Move to $destination")) {
      Ensure-Directory -Path $destinationDirectory

      Move-Item -LiteralPath $item.FullName -Destination $destination -ErrorAction Stop

      [pscustomobject]@{
        Source      = $item.FullName
        Destination = $destination
        Status      = "Moved"
        Message     = ""
      }
    }
    else {
      [pscustomobject]@{
        Source      = $item.FullName
        Destination = $destination
        Status      = "Preview"
        Message     = ""
      }
    }
  }
}

$config = Import-LogMaintenanceConfig -Path $ConfigPath

$runStamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportDir = Join-Path ([string]$config.OutputPath) $runStamp
Ensure-Directory -Path $reportDir

$transcriptStarted = $false
$transcriptPath = Join-Path $reportDir "transcript.txt"

try {
  if (-not $SkipTranscript) {
    Start-Transcript -Path $transcriptPath -Force | Out-Null
    $transcriptStarted = $true
  }

  Write-Host "Report directory: $reportDir"

  $hits = @(
    Get-LogHit `
      -LogPath ([string]$config.LogPath) `
      -Days ([int]$config.Days) `
      -Pattern ([string[]]$config.Patterns)
  )

  $hitCsv = Join-Path $reportDir "log-hits.csv"
  Export-CsvWithHeader `
    -InputObject $hits `
    -Path $hitCsv `
    -Header @("Path", "LineNumber", "Pattern", "Line")

  $oldFiles = @(
    Get-OldLogFile `
      -LogPath ([string]$config.LogPath) `
      -ArchiveDays ([int]$config.ArchiveDays)
  )

  $archiveTargets = @(
    $oldFiles |
      Select-Object FullName, Length, LastWriteTime
  )

  $archiveTargetCsv = Join-Path $reportDir "archive-targets.csv"
  Export-CsvWithHeader `
    -InputObject $archiveTargets `
    -Path $archiveTargetCsv `
    -Header @("FullName", "Length", "LastWriteTime")

  $moveResults = @()

  if ($SkipArchive) {
    Write-Host "Archive skipped."
  }
  elseif ($oldFiles.Count -eq 0) {
    Write-Host "No archive targets."
  }
  else {
    $archiveRunRoot = Join-Path ([string]$config.ArchivePath) $runStamp

    $moveResults = @(
      Move-OldLogFile `
        -File $oldFiles `
        -SourceRoot ([string]$config.LogPath) `
        -ArchiveRoot $archiveRunRoot `
        -WhatIf:$Preview
    )
  }

  $archiveResultCsv = Join-Path $reportDir "archive-result.csv"
  Export-CsvWithHeader `
    -InputObject $moveResults `
    -Path $archiveResultCsv `
    -Header @("Source", "Destination", "Status", "Message")

  $summary = [pscustomobject]@{
    CheckedAt          = (Get-Date).ToString("s")
    ComputerName       = [System.Environment]::MachineName
    LogPath            = [string]$config.LogPath
    ReportDirectory    = $reportDir
    HitCount           = $hits.Count
    ArchiveTargetCount = $oldFiles.Count
    ArchiveResultCount = $moveResults.Count
    Preview            = [bool]$Preview
    SkipArchive        = [bool]$SkipArchive
  }

  $summaryPath = Join-Path $reportDir "summary.json"
  $summary |
    ConvertTo-Json -Depth 5 |
    Set-Content -LiteralPath $summaryPath -Encoding UTF8

  Write-Host "Finished."
  Write-Host "Hits: $($hits.Count)"
  Write-Host "Archive targets: $($oldFiles.Count)"
}
catch {
  $errorPath = Join-Path $reportDir "error.txt"
  $_ | Out-String | Set-Content -LiteralPath $errorPath -Encoding UTF8
  Write-Error "Failed: $($_.Exception.Message)"
  exit 1
}
finally {
  if ($transcriptStarted) {
    Stop-Transcript | Out-Null
  }
}
