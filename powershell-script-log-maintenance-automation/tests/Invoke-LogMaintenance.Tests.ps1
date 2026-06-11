BeforeAll {
    $script:PwshPath = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    $script:ScriptPath = Join-Path $PSScriptRoot '../src/Invoke-LogMaintenance.ps1'

    # Logs / Reports / Archive とダミーログ、設定 JSON を一時ディレクトリに作る
    function New-LogSandbox {
        param(
            [Parameter(Mandatory)]
            [string]$Root
        )

        $logPath = Join-Path $Root 'Logs'
        $outputPath = Join-Path $Root 'Reports'
        $archivePath = Join-Path $Root 'Archive'

        foreach ($path in @($logPath, (Join-Path $logPath 'old'), $outputPath, $archivePath)) {
            New-Item -ItemType Directory -Path $path -Force | Out-Null
        }

        # 直近のログ（ERROR / WARN / FATAL を 1 行ずつ含む。合計 3 ヒット）
        @(
            'INFO  application started'
            'ERROR failed to connect to database'
            'WARN  disk usage 85 percent'
        ) | Set-Content -LiteralPath (Join-Path $logPath 'app.log') -Encoding UTF8

        @(
            'INFO  batch started'
            'FATAL batch aborted'
        ) | Set-Content -LiteralPath (Join-Path $logPath 'batch.log') -Encoding UTF8

        # 古いログ（ArchiveDays = 90 より古い 120 日前に設定）
        $oldLog = Join-Path $logPath 'old/app-202401.log'
        'ERROR old error entry' | Set-Content -LiteralPath $oldLog -Encoding UTF8
        (Get-Item -LiteralPath $oldLog).LastWriteTime = (Get-Date).AddDays(-120)

        $configPath = Join-Path $Root 'log-maintenance.json'
        [ordered]@{
            LogPath     = $logPath
            OutputPath  = $outputPath
            Days        = 7
            Patterns    = @('ERROR', 'WARN', 'FATAL')
            ArchiveDays = 90
            ArchivePath = $archivePath
        } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $configPath -Encoding UTF8

        [pscustomobject]@{
            Root        = $Root
            LogPath     = $logPath
            OutputPath  = $outputPath
            ArchivePath = $archivePath
            ConfigPath  = $configPath
            OldLog      = $oldLog
        }
    }

    # スクリプトを子プロセスの pwsh で実行する（catch 内の exit 1 をテスト側に波及させないため）
    function Invoke-LogMaintenanceScript {
        param(
            [Parameter(Mandatory)]
            [string]$ConfigPath,

            [string[]]$AdditionalArguments = @()
        )

        $output = & $script:PwshPath -NoProfile -File $script:ScriptPath -ConfigPath $ConfigPath @AdditionalArguments 2>&1

        [pscustomobject]@{
            ExitCode = $LASTEXITCODE
            Output   = ($output | Out-String)
        }
    }

    function Get-ReportDirectory {
        param(
            [Parameter(Mandatory)]
            [string]$OutputPath
        )

        Get-ChildItem -LiteralPath $OutputPath -Directory |
            Sort-Object Name |
            Select-Object -Last 1
    }
}

Describe 'Invoke-LogMaintenance.ps1' {
    BeforeEach {
        $script:Sandbox = New-LogSandbox -Root (Join-Path $TestDrive ([guid]::NewGuid().ToString('N')))
    }

    It '-SkipArchive ではログ調査だけを行い、ファイルを移動しない' {
        $result = Invoke-LogMaintenanceScript `
            -ConfigPath $script:Sandbox.ConfigPath `
            -AdditionalArguments @('-SkipArchive', '-SkipTranscript')

        $result.ExitCode | Should -Be 0
        Test-Path -LiteralPath $script:Sandbox.OldLog | Should -BeTrue

        $reportDir = Get-ReportDirectory -OutputPath $script:Sandbox.OutputPath
        $reportDir | Should -Not -BeNullOrEmpty

        $moveResults = @(Import-Csv -LiteralPath (Join-Path $reportDir.FullName 'archive-result.csv'))
        $moveResults | Should -HaveCount 0

        Get-ChildItem -LiteralPath $script:Sandbox.ArchivePath -Recurse -File | Should -HaveCount 0
    }

    It '直近ログの ERROR / WARN / FATAL 行が log-hits.csv に出力される' {
        $result = Invoke-LogMaintenanceScript `
            -ConfigPath $script:Sandbox.ConfigPath `
            -AdditionalArguments @('-SkipArchive', '-SkipTranscript')

        $result.ExitCode | Should -Be 0

        $reportDir = Get-ReportDirectory -OutputPath $script:Sandbox.OutputPath
        $hits = @(Import-Csv -LiteralPath (Join-Path $reportDir.FullName 'log-hits.csv'))

        $hits | Should -HaveCount 3
        $hits.Pattern | Should -Contain 'ERROR'
        $hits.Pattern | Should -Contain 'WARN'
        $hits.Pattern | Should -Contain 'FATAL'

        # 古いログは Days = 7 の調査対象外（ERROR を含んでいてもヒットしない）
        $hits.Path | Should -Not -Contain $script:Sandbox.OldLog
    }

    It 'ArchiveDays より古いログが archive-targets.csv に一覧化される' {
        $result = Invoke-LogMaintenanceScript `
            -ConfigPath $script:Sandbox.ConfigPath `
            -AdditionalArguments @('-SkipArchive', '-SkipTranscript')

        $result.ExitCode | Should -Be 0

        $reportDir = Get-ReportDirectory -OutputPath $script:Sandbox.OutputPath
        $targets = @(Import-Csv -LiteralPath (Join-Path $reportDir.FullName 'archive-targets.csv'))

        $targets | Should -HaveCount 1
        $targets[0].FullName | Should -Be (Get-Item -LiteralPath $script:Sandbox.OldLog).FullName
    }

    It '-Preview では移動予定だけを記録し、実際には移動しない' {
        $result = Invoke-LogMaintenanceScript `
            -ConfigPath $script:Sandbox.ConfigPath `
            -AdditionalArguments @('-Preview', '-SkipTranscript')

        $result.ExitCode | Should -Be 0
        Test-Path -LiteralPath $script:Sandbox.OldLog | Should -BeTrue
        Get-ChildItem -LiteralPath $script:Sandbox.ArchivePath -Recurse -File | Should -HaveCount 0

        $reportDir = Get-ReportDirectory -OutputPath $script:Sandbox.OutputPath
        $moveResults = @(Import-Csv -LiteralPath (Join-Path $reportDir.FullName 'archive-result.csv'))

        $moveResults | Should -HaveCount 1
        $moveResults[0].Status | Should -Be 'Preview'
        $moveResults[0].Source | Should -Be (Get-Item -LiteralPath $script:Sandbox.OldLog).FullName
    }

    It '本実行では古いログを相対パスを保ったままアーカイブへ移動する' {
        $result = Invoke-LogMaintenanceScript `
            -ConfigPath $script:Sandbox.ConfigPath `
            -AdditionalArguments @('-SkipTranscript')

        $result.ExitCode | Should -Be 0
        Test-Path -LiteralPath $script:Sandbox.OldLog | Should -BeFalse

        $moved = @(Get-ChildItem -LiteralPath $script:Sandbox.ArchivePath -Recurse -File)
        $moved | Should -HaveCount 1
        $moved[0].Name | Should -Be 'app-202401.log'
        $moved[0].Directory.Name | Should -Be 'old'

        $reportDir = Get-ReportDirectory -OutputPath $script:Sandbox.OutputPath
        $moveResults = @(Import-Csv -LiteralPath (Join-Path $reportDir.FullName 'archive-result.csv'))

        $moveResults | Should -HaveCount 1
        $moveResults[0].Status | Should -Be 'Moved'
        $moveResults[0].Destination | Should -Be $moved[0].FullName
    }

    It 'summary.json に件数と実行条件が記録される' {
        $result = Invoke-LogMaintenanceScript `
            -ConfigPath $script:Sandbox.ConfigPath `
            -AdditionalArguments @('-Preview', '-SkipTranscript')

        $result.ExitCode | Should -Be 0

        $reportDir = Get-ReportDirectory -OutputPath $script:Sandbox.OutputPath
        $summary = Get-Content -LiteralPath (Join-Path $reportDir.FullName 'summary.json') -Raw |
            ConvertFrom-Json

        $summary.HitCount | Should -Be 3
        $summary.ArchiveTargetCount | Should -Be 1
        $summary.ArchiveResultCount | Should -Be 1
        $summary.Preview | Should -BeTrue
        $summary.SkipArchive | Should -BeFalse
        $summary.LogPath | Should -Be $script:Sandbox.LogPath
    }

    It '既定では transcript.txt が証跡として残る' {
        $result = Invoke-LogMaintenanceScript `
            -ConfigPath $script:Sandbox.ConfigPath `
            -AdditionalArguments @('-SkipArchive')

        $result.ExitCode | Should -Be 0

        $reportDir = Get-ReportDirectory -OutputPath $script:Sandbox.OutputPath
        Test-Path -LiteralPath (Join-Path $reportDir.FullName 'transcript.txt') | Should -BeTrue
    }

    It '設定ファイルに必須項目が無い場合は失敗する' {
        $config = Get-Content -LiteralPath $script:Sandbox.ConfigPath -Raw | ConvertFrom-Json
        $config.PSObject.Properties.Remove('ArchivePath')
        $config | ConvertTo-Json -Depth 5 |
            Set-Content -LiteralPath $script:Sandbox.ConfigPath -Encoding UTF8

        $result = Invoke-LogMaintenanceScript `
            -ConfigPath $script:Sandbox.ConfigPath `
            -AdditionalArguments @('-SkipTranscript')

        $result.ExitCode | Should -Not -Be 0
        $result.Output | Should -Match 'Config value missing: ArchivePath'
    }

    It 'LogPath が存在しない場合は失敗する' {
        $config = Get-Content -LiteralPath $script:Sandbox.ConfigPath -Raw | ConvertFrom-Json
        $config.LogPath = Join-Path $script:Sandbox.Root 'missing'
        $config | ConvertTo-Json -Depth 5 |
            Set-Content -LiteralPath $script:Sandbox.ConfigPath -Encoding UTF8

        $result = Invoke-LogMaintenanceScript `
            -ConfigPath $script:Sandbox.ConfigPath `
            -AdditionalArguments @('-SkipTranscript')

        $result.ExitCode | Should -Not -Be 0
        $result.Output | Should -Match 'LogPath not found'
    }
}
