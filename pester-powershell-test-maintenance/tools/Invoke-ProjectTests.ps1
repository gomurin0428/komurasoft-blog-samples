$ErrorActionPreference = 'Stop'

Import-Module Pester

$config = New-PesterConfiguration

$config.Run.Path = @(
    Join-Path $PSScriptRoot '../tests'
)

$config.Run.Exit = $true
$config.Output.Verbosity = 'Detailed'

$config.TestResult.Enabled = $true
$config.TestResult.OutputFormat = 'JUnitXml'
$config.TestResult.OutputPath = Join-Path $PSScriptRoot '../test-results.xml'

$config.CodeCoverage.Enabled = $true
$config.CodeCoverage.Path = @(
    Join-Path $PSScriptRoot '../src'
)
$config.CodeCoverage.OutputPath = Join-Path $PSScriptRoot '../coverage.xml'

Invoke-Pester -Configuration $config
