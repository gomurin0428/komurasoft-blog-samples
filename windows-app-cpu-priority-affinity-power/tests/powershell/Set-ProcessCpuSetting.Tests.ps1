BeforeAll {
    . "$PSScriptRoot/../../src/powershell/Set-ProcessCpuSetting.ps1"

    # 自分自身（テストランナー）の優先度やアフィニティを変えないよう、
    # 検証対象として待機するだけの子プロセス（pwsh）を使う
    function script:Start-SleepingChildProcess {
        $pwshPath = (Get-Process -Id $PID).Path

        Start-Process `
            -FilePath $pwshPath `
            -ArgumentList '-NoProfile', '-Command', 'Start-Sleep -Seconds 120' `
            -PassThru
    }
}

Describe 'Set-ProcessCpuSetting' {
    Context 'パラメーター検証' {
        It '優先度もアフィニティも指定しないと失敗する' {
            { Set-ProcessCpuSetting -Id $PID } | Should -Throw
        }

        It 'AffinityMask に 0 を指定すると失敗する' {
            { Set-ProcessCpuSetting -Id $PID -AffinityMask 0 } | Should -Throw
        }

        It 'AffinityMask に負の値を指定すると失敗する' {
            { Set-ProcessCpuSetting -Id $PID -AffinityMask -1 } | Should -Throw
        }

        It '存在しないプロセス ID では失敗する' {
            $missingId = 0x7FFFFFF0

            { Set-ProcessCpuSetting -Id $missingId -PriorityClass Normal } |
                Should -Throw
        }
    }

    Context '子プロセスへの適用（Windows / Linux で実行検証）' {
        BeforeEach {
            $script:Child = Start-SleepingChildProcess
        }

        AfterEach {
            if ($script:Child -and -not $script:Child.HasExited) {
                Stop-Process -Id $script:Child.Id -Force -ErrorAction SilentlyContinue
            }
        }

        It '-WhatIf では優先度を変更しない' -Skip:(-not ($IsWindows -or $IsLinux)) {
            $before = (Get-Process -Id $script:Child.Id).PriorityClass

            Set-ProcessCpuSetting `
                -Id $script:Child.Id `
                -PriorityClass BelowNormal `
                -WhatIf | Out-Null

            (Get-Process -Id $script:Child.Id).PriorityClass | Should -Be $before
        }

        It '優先度クラスを設定できる' -Skip:(-not ($IsWindows -or $IsLinux)) {
            # 権限なしでも通るよう、下げる方向（BelowNormal）で検証する
            Set-ProcessCpuSetting `
                -Id $script:Child.Id `
                -PriorityClass BelowNormal | Out-Null

            (Get-Process -Id $script:Child.Id).PriorityClass |
                Should -Be ([System.Diagnostics.ProcessPriorityClass]::BelowNormal)
        }

        It 'アフィニティマスクを設定できる' -Skip:(-not ($IsWindows -or $IsLinux)) {
            # 0x1 = 論理プロセッサ 0 のみを許可
            Set-ProcessCpuSetting `
                -Id $script:Child.Id `
                -AffinityMask 0x1 | Out-Null

            (Get-Process -Id $script:Child.Id).ProcessorAffinity |
                Should -Be ([IntPtr]1)
        }

        It '変更後の状態（Id, ProcessName, PriorityClass, ProcessorAffinity）を返す' -Skip:(-not ($IsWindows -or $IsLinux)) {
            $result = Set-ProcessCpuSetting `
                -Id $script:Child.Id `
                -PriorityClass BelowNormal

            $result.Id | Should -Be $script:Child.Id

            $propertyNames = $result.PSObject.Properties.Name
            $propertyNames | Should -Contain 'ProcessName'
            $propertyNames | Should -Contain 'PriorityClass'
            $propertyNames | Should -Contain 'ProcessorAffinity'
        }
    }
}
