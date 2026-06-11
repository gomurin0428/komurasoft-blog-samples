BeforeAll {
    . "$PSScriptRoot/../src/Get-OldLogFile.ps1"
    . "$PSScriptRoot/../src/Remove-OldLogFile.ps1"
}

Describe 'Remove-OldLogFile' {
    It '古いログファイルに対して Remove-Item を呼ぶ' {
        Mock Get-OldLogFile {
            [pscustomobject]@{
                FullName      = 'C:\Logs\old.log'
                Name          = 'old.log'
                Length        = 10
                LastWriteTime = [datetime]'2026-05-01'
            }
        }

        Mock Remove-Item {}

        Remove-OldLogFile `
            -Path 'C:\Logs' `
            -Days 30 `
            -Now ([datetime]'2026-06-01')

        Should -Invoke Remove-Item `
            -Times 1 `
            -Exactly `
            -ParameterFilter { $LiteralPath -eq 'C:\Logs\old.log' }
    }

    It 'WhatIf では Remove-Item を呼ばない' {
        Mock Get-OldLogFile {
            [pscustomobject]@{
                FullName      = 'C:\Logs\old.log'
                Name          = 'old.log'
                Length        = 10
                LastWriteTime = [datetime]'2026-05-01'
            }
        }

        Mock Remove-Item {}

        Remove-OldLogFile `
            -Path 'C:\Logs' `
            -Days 30 `
            -Now ([datetime]'2026-06-01') `
            -WhatIf

        Should -Invoke Remove-Item -Times 0
    }
}
