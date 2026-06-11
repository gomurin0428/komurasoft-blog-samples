Describe 'Export-ErrorReport.ps1（記事 21 章）' {
    BeforeAll {
        $script:ScriptPath = "$PSScriptRoot/../src/Export-ErrorReport.ps1"

        $script:LogRoot = Join-Path $TestDrive 'app-logs'
        New-Item -ItemType Directory -Path $script:LogRoot -Force | Out-Null

        @(
            'INFO  started'
            'ERROR failed to connect'
            'INFO  retrying'
            'ERROR timeout'
        ) | Set-Content (Join-Path $script:LogRoot 'app.log') -Encoding UTF8

        'INFO  all good' | Set-Content (Join-Path $script:LogRoot 'clean.log') -Encoding UTF8

        # 7日より前に更新されたログ。ERROR を含んでいても対象外になるべき
        'ERROR too old to report' |
            Set-Content (Join-Path $script:LogRoot 'old.log') -Encoding UTF8
        (Get-Item (Join-Path $script:LogRoot 'old.log')).LastWriteTime = (Get-Date).AddDays(-30)

        $script:OutputPath = Join-Path $TestDrive 'error-report.csv'

        & $script:ScriptPath `
            -LogPath $script:LogRoot `
            -Days 7 `
            -Pattern 'ERROR' `
            -OutputPath $script:OutputPath *> $null
    }

    It '直近7日以内のログの ERROR 行だけが出力される' {
        $rows = Import-Csv $script:OutputPath

        $rows | Should -HaveCount 2
        $rows.Line | ForEach-Object { $_ | Should -Match 'ERROR' }
        ($rows.Path | Split-Path -Leaf | Select-Object -Unique) | Should -Be 'app.log'
    }

    It '期間外のファイルは ERROR を含んでいても対象外になる' {
        $names = Import-Csv $script:OutputPath |
            ForEach-Object { Split-Path $_.Path -Leaf }

        $names | Should -Not -Contain 'old.log'
    }

    It 'Path / LineNumber / Line の列を持ち、行番号が取れている' {
        $rows = Import-Csv $script:OutputPath

        $rows[0].PSObject.Properties.Name | Should -Contain 'Path'
        $rows.LineNumber | Should -Contain '2'
        $rows.LineNumber | Should -Contain '4'
    }
}
