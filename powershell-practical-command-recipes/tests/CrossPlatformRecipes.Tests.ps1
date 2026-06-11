BeforeAll {
    $script:SrcRoot = Join-Path $PSScriptRoot '../src'
}

Describe 'file-recipes.ps1' {
    BeforeAll {
        $script:WorkRoot = Join-Path $TestDrive 'files'
        $null = & (Join-Path $script:SrcRoot 'file-recipes.ps1') -WorkRoot $script:WorkRoot 6> $null
    }

    It 'ファイル名一覧を file-names.txt に保存する（6 章）' {
        $names = Get-Content (Join-Path $script:WorkRoot 'file-names.txt')
        $names | Should -Contain 'report.txt'
        $names | Should -Contain 'app.log'
    }

    It '削除対象を delete-targets.csv に残す（34 章）' {
        $rows = Import-Csv (Join-Path $script:WorkRoot 'delete-targets.csv')
        $rows | Should -HaveCount 2
        ($rows.FullName | Split-Path -Leaf) | Sort-Object | Should -Be @('old1.tmp', 'old2.tmp')
    }

    It '30 日より古い .tmp だけを削除する（34 章）' {
        Test-Path (Join-Path $script:WorkRoot 'old1.tmp') | Should -BeFalse
        Test-Path (Join-Path $script:WorkRoot 'old2.tmp') | Should -BeFalse
        Test-Path (Join-Path $script:WorkRoot 'new.tmp') | Should -BeTrue
    }
}

Describe 'log-search-recipes.ps1' {
    BeforeAll {
        $script:WorkRoot = Join-Path $TestDrive 'logs'
        $null = & (Join-Path $script:SrcRoot 'log-search-recipes.ps1') -WorkRoot $script:WorkRoot 6> $null
    }

    It 'ERROR を含む行を error-lines.csv に残す（12 章）' {
        $rows = Import-Csv (Join-Path $script:WorkRoot 'error-lines.csv')
        $rows.Count | Should -BeGreaterOrEqual 1
        $rows[0].PSObject.Properties.Name | Should -Contain 'LineNumber'
        $rows.Line | Should -Contain '2026-06-01 09:00:06 ERROR connection failed'
    }

    It '置換結果を元ファイルではなく別ファイルに出す（13 章）' {
        $preview = Get-Content (Join-Path $script:WorkRoot 'appsettings.preview.json') -Raw
        $preview | Should -Match 'db01'
        $preview | Should -Not -Match 'localhost'

        # 元ファイルは書き換えない
        Get-Content (Join-Path $script:WorkRoot 'appsettings.json') -Raw |
            Should -Match 'localhost'
    }
}

Describe 'data-recipes.ps1' {
    BeforeAll {
        $script:WorkRoot = Join-Path $TestDrive 'data'
        $script:Output = & (Join-Path $script:SrcRoot 'data-recipes.ps1') -WorkRoot $script:WorkRoot 6> $null
    }

    It '有効なユーザーだけを enabled-users.csv に出す（19 章）' {
        $rows = Import-Csv (Join-Path $script:WorkRoot 'enabled-users.csv')
        $rows | Should -HaveCount 2
        $rows.Name | Sort-Object | Should -Be @('Sato', 'Suzuki')
    }

    It 'JSON の値を変更して別ファイルに保存する（20 章）' {
        $updated = Get-Content (Join-Path $script:WorkRoot 'settings.updated.json') -Raw |
            ConvertFrom-Json
        $updated.Database.Host | Should -Be 'db01'

        # 元ファイルは書き換えない
        $original = Get-Content (Join-Path $script:WorkRoot 'settings.json') -Raw |
            ConvertFrom-Json
        $original.Database.Host | Should -Be 'localhost'
    }

    It 'Compare-Object が両フォルダーの差分を返す（14 章）' {
        $diff = $script:Output | Where-Object { $_.PSObject.Properties.Name -contains 'SideIndicator' }
        ($diff | Where-Object SideIndicator -eq '<=').InputObject | Should -Contain 'removed.txt'
        ($diff | Where-Object SideIndicator -eq '=>').InputObject | Should -Contain 'added.txt'
    }

    It 'ハッシュ値を hashes.csv に残す（15 章）' {
        $rows = Import-Csv (Join-Path $script:WorkRoot 'hashes.csv')
        $rows | Should -HaveCount 2
        $rows[0].Hash | Should -Match '^[0-9A-F]{64}$'
    }
}

Describe 'process-job-recipes.ps1' {
    It '一連のプロセス・計測・ジョブのレシピがエラーなく実行できる' {
        $workRoot = Join-Path $TestDrive 'process'

        { $script:Output = & (Join-Path $script:SrcRoot 'process-job-recipes.ps1') -WorkRoot $workRoot 6> $null } |
            Should -Not -Throw

        # Measure-Command の結果（TimeSpan）が出力に含まれる（28 章）
        $script:Output | Where-Object { $_ -is [timespan] } | Should -Not -BeNullOrEmpty
    }
}

Describe 'output-recipes.ps1' {
    BeforeAll {
        $script:WorkRoot = Join-Path $TestDrive 'output'
        # Write-Host の内容が transcript に記録されることを確認するため、
        # ここでは情報ストリーム（6）をリダイレクトせずに実行する
        $null = & (Join-Path $script:SrcRoot 'output-recipes.ps1') -WorkRoot $script:WorkRoot 3> $null
    }

    It 'Select-Object 経由の CSV にプロパティ列が出る（11 章）' {
        $rows = Import-Csv (Join-Path $script:WorkRoot 'process.csv')
        $rows.Count | Should -BeGreaterOrEqual 1
        $rows[0].PSObject.Properties.Name | Should -Be @('Name', 'Id')
    }

    It 'Tee-Object が画面とファイルの両方に出す（17 章）' {
        Test-Path (Join-Path $script:WorkRoot 'top-process.txt') | Should -BeTrue
        (Import-Csv (Join-Path $script:WorkRoot 'top-process.csv')).Count |
            Should -BeGreaterOrEqual 1
    }

    It 'Start-Transcript が work-log.txt に操作を記録する（18 章）' {
        $log = Get-Content (Join-Path $script:WorkRoot 'work-log.txt') -Raw
        $log | Should -Match 'PowerShell transcript start'
        $log | Should -Match 'File count: \d+'
    }
}

Describe 'environment-recipes.ps1' {
    It '環境変数とプロファイルのレシピがエラーなく実行できる' {
        { $null = & (Join-Path $script:SrcRoot 'environment-recipes.ps1') } |
            Should -Not -Throw

        # セッション内の環境変数設定（21 章）が反映されている
        $env:APP_MODE | Should -Be 'Development'
    }
}

Describe 'windows-recipes.ps1' -Skip:$IsWindows {
    It 'Windows 以外では警告を出して終了する' {
        $warnings = $null
        $output = & (Join-Path $script:SrcRoot 'windows-recipes.ps1') -WarningVariable warnings 3> $null

        $output | Should -BeNullOrEmpty
        $warnings | Should -Not -BeNullOrEmpty
    }
}
