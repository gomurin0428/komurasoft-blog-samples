BeforeAll {
    $script:ScriptPath = Join-Path $PSScriptRoot '../../src/powershell/Get-WindowsPowerInfo.ps1'

    . $script:ScriptPath
}

Describe 'Get-WindowsPowerInfo.ps1' {
    Context '構文解析（全プラットフォームで検証）' {
        It '構文エラーなく解析できる' {
            $tokens = $null
            $errors = $null

            [System.Management.Automation.Language.Parser]::ParseFile(
                $script:ScriptPath,
                [ref] $tokens,
                [ref] $errors) | Out-Null

            $errors | Should -HaveCount 0
        }

        It '記事 10 章のコマンドに対応する関数を定義している' {
            Get-Command Get-CpuInfo -ErrorAction Stop | Should -Not -BeNullOrEmpty
            Get-Command Get-ActivePowerScheme -ErrorAction Stop | Should -Not -BeNullOrEmpty
            Get-Command Get-ProcessorPowerSetting -ErrorAction Stop | Should -Not -BeNullOrEmpty
            Get-Command New-PowerEnergyReport -ErrorAction Stop | Should -Not -BeNullOrEmpty
            Get-Command Get-CpuPerformanceCounter -ErrorAction Stop | Should -Not -BeNullOrEmpty
        }

        It 'New-PowerEnergyReport は -WhatIf で予行できる（SupportsShouldProcess）' {
            (Get-Command New-PowerEnergyReport).Parameters.Keys |
                Should -Contain 'WhatIf'
        }
    }

    Context 'Windows 以外でのガード' {
        It 'Get-CpuInfo は Windows 以外では明確なエラーで失敗する' -Skip:$IsWindows {
            { Get-CpuInfo } | Should -Throw '*only on Windows*'
        }

        It 'Get-ActivePowerScheme は Windows 以外では明確なエラーで失敗する' -Skip:$IsWindows {
            { Get-ActivePowerScheme } | Should -Throw '*only on Windows*'
        }

        It 'Get-ProcessorPowerSetting は Windows 以外では明確なエラーで失敗する' -Skip:$IsWindows {
            { Get-ProcessorPowerSetting } | Should -Throw '*only on Windows*'
        }

        It 'New-PowerEnergyReport は Windows 以外では明確なエラーで失敗する' -Skip:$IsWindows {
            { New-PowerEnergyReport -WhatIf } | Should -Throw '*only on Windows*'
        }

        It 'Get-CpuPerformanceCounter は Windows 以外では明確なエラーで失敗する' -Skip:$IsWindows {
            { Get-CpuPerformanceCounter } | Should -Throw '*only on Windows*'
        }
    }
}
