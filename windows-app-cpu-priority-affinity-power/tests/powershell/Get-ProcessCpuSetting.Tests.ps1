BeforeAll {
    . "$PSScriptRoot/../../src/powershell/Get-ProcessCpuSetting.ps1"
}

Describe 'Get-ProcessCpuSetting' {
    It '既定では現在のプロセス（$PID）の情報を返す' {
        $result = Get-ProcessCpuSetting

        $result | Should -HaveCount 1
        $result.Id | Should -Be $PID
        $result.ProcessName | Should -Not -BeNullOrEmpty
    }

    It '記事のコマンドと同じプロパティ（Id, ProcessName, PriorityClass, ProcessorAffinity）を返す' {
        $result = Get-ProcessCpuSetting -Id $PID

        $propertyNames = $result.PSObject.Properties.Name

        $propertyNames | Should -Contain 'Id'
        $propertyNames | Should -Contain 'ProcessName'
        $propertyNames | Should -Contain 'PriorityClass'
        $propertyNames | Should -Contain 'ProcessorAffinity'
    }

    It 'PriorityClass と ProcessorAffinity に値が入る（Windows / Linux）' -Skip:(-not ($IsWindows -or $IsLinux)) {
        $result = Get-ProcessCpuSetting -Id $PID

        $result.PriorityClass | Should -Not -BeNullOrEmpty
        $result.ProcessorAffinity | Should -Not -BeNullOrEmpty
    }

    It 'プロセス名でも取得できる' {
        $currentName = (Get-Process -Id $PID).ProcessName

        $result = Get-ProcessCpuSetting -Name $currentName

        $result.Id | Should -Contain $PID
    }

    It '存在しないプロセス ID では失敗する' {
        # 0 は OS 側の特殊なプロセスのため、未使用の大きな ID を使う
        $missingId = 0x7FFFFFF0

        { Get-ProcessCpuSetting -Id $missingId } | Should -Throw
    }
}
