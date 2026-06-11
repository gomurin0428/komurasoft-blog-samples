BeforeDiscovery {
    $script:demoScripts = Get-ChildItem -Path "$PSScriptRoot/../src" -Filter '*.ps1' |
        Where-Object Name -Match '^\d{2}-' |
        ForEach-Object { @{ Name = $_.Name; Path = $_.FullName } }
}

Describe 'テーマ別スクリプトの実行' {
    It '<Name> が一時ワークスペース上でエラーなく実行できる' -ForEach $demoScripts {
        $workspace = Join-Path $TestDrive ([System.IO.Path]::GetFileNameWithoutExtension($Name))

        { & $Path -WorkspacePath $workspace *> $null } | Should -Not -Throw
    }
}

Describe 'テーマ別スクリプトの動作検証' {
    Context '04-file-folder-operations.ps1（記事 8 章）' {
        BeforeAll {
            $script:Workspace = Join-Path $TestDrive 'verify-04'
            & "$PSScriptRoot/../src/04-file-folder-operations.ps1" -WorkspacePath $script:Workspace *> $null
        }

        It 'archive フォルダーが作成され、users.csv がコピーされている' {
            Join-Path $script:Workspace 'archive/users.csv' | Should -Exist
        }

        It '30日より古い .log が archive へ移動されている' {
            Join-Path $script:Workspace 'archive/batch.log' | Should -Exist
            Join-Path $script:Workspace 'logs/batch.log' | Should -Not -Exist
        }

        It 'memo.txt と logs 配下の .tmp が削除されている' {
            Join-Path $script:Workspace 'memo.txt' | Should -Not -Exist
            Get-ChildItem (Join-Path $script:Workspace 'logs') -Filter *.tmp -Recurse |
                Should -BeNullOrEmpty
        }
    }

    Context '05-text-search-write.ps1（記事 9 章）' {
        BeforeAll {
            $script:Workspace = Join-Path $TestDrive 'verify-05'
            & "$PSScriptRoot/../src/05-text-search-write.ps1" -WorkspacePath $script:Workspace *> $null
        }

        It 'ERROR 行だけが error-lines.csv に出力されている' {
            $rows = Import-Csv (Join-Path $script:Workspace 'error-lines.csv')

            $rows | Should -HaveCount 3
            $rows.Line | ForEach-Object { $_ | Should -Match 'ERROR' }
        }

        It 'error-lines.csv に Path / LineNumber / Line の列がある' {
            $rows = Import-Csv (Join-Path $script:Workspace 'error-lines.csv')
            $propertyNames = $rows[0].PSObject.Properties.Name

            $propertyNames | Should -Contain 'Path'
            $propertyNames | Should -Contain 'LineNumber'
            $propertyNames | Should -Contain 'Line'
        }

        It 'Set-Content と Add-Content で memo.txt が2行になっている' {
            Get-Content (Join-Path $script:Workspace 'memo.txt') |
                Should -Be @('hello', 'next line')
        }
    }

    Context '06-csv-json.ps1（記事 10〜11 章）' {
        BeforeAll {
            $script:Workspace = Join-Path $TestDrive 'verify-06'
            & "$PSScriptRoot/../src/06-csv-json.ps1" -WorkspacePath $script:Workspace *> $null
        }

        It 'Sales の2人だけが sales-users.csv に出力されている' {
            $rows = Import-Csv (Join-Path $script:Workspace 'sales-users.csv')

            $rows | Should -HaveCount 2
            $rows.Name | Should -Contain 'Suzuki'
            $rows.Name | Should -Contain 'Sato'
        }

        It '書き戻した settings.json を再度 JSON として読み込める' {
            $data = Get-Content (Join-Path $script:Workspace 'settings.json') -Raw |
                ConvertFrom-Json

            $data.Name | Should -Be 'BatchJob'
            $data.Retry | Should -Be 3
        }
    }

    Context '09-execution-policy-and-errors.ps1（記事 17〜18 章）' {
        BeforeAll {
            $script:Workspace = Join-Path $TestDrive 'verify-09'
            & "$PSScriptRoot/../src/09-execution-policy-and-errors.ps1" -WorkspacePath $script:Workspace *> $null
        }

        It 'input の2ファイルが OK として copy-result.csv に記録されている' {
            $rows = Import-Csv (Join-Path $script:Workspace 'copy-result.csv')

            $rows | Should -HaveCount 2
            $rows.Status | ForEach-Object { $_ | Should -Be 'OK' }
        }

        It 'backup フォルダーにコピーが作られている' {
            Join-Path $script:Workspace 'backup/source.txt' | Should -Exist
            Join-Path $script:Workspace 'backup/a.txt' | Should -Exist
            Join-Path $script:Workspace 'backup/b.txt' | Should -Exist
        }
    }

    Context '10-safe-change-workflow.ps1（記事 19・22 章）' {
        BeforeAll {
            $script:Workspace = Join-Path $TestDrive 'verify-10'
            & "$PSScriptRoot/../src/10-safe-change-workflow.ps1" -WorkspacePath $script:Workspace *> $null
        }

        It '30日より古い .tmp だけが削除されている' {
            Join-Path $script:Workspace 'temp/old1.tmp' | Should -Not -Exist
            Join-Path $script:Workspace 'temp/old2.tmp' | Should -Not -Exist
            Join-Path $script:Workspace 'temp/recent.tmp' | Should -Exist
        }

        It '削除前の証跡 delete-targets.csv が残っている' {
            $rows = Import-Csv (Join-Path $script:Workspace 'delete-targets.csv')

            $rows | Should -HaveCount 2
            $rows.FullName | ForEach-Object { $_ | Should -Match '\.tmp$' }
        }

        It '90日以上更新されていない .xlsx だけがアーカイブへ移動されている' {
            Join-Path $script:Workspace 'reports-archive/sales-2025.xlsx' | Should -Exist
            Join-Path $script:Workspace 'reports/sales-2026.xlsx' | Should -Exist
            Join-Path $script:Workspace 'reports/sales-2025.xlsx' | Should -Not -Exist
        }
    }
}
