Describe 'Windows 専用スクリプトの静的検証' {
    It '<_> はパースエラーなしで解析できる' -ForEach @(
        'Find-VbScriptFileDependency.ps1'
        'Get-VbScriptScheduledTaskDependency.ps1'
        'Get-VbScriptExecutionLog.ps1'
        'Normalize.ps1'
        'Set-ScriptSignature.ps1'
    ) {
        $tokens = $null
        $errors = $null
        [System.Management.Automation.Language.Parser]::ParseFile(
            "$PSScriptRoot/../src/$_", [ref]$tokens, [ref]$errors) | Out-Null

        $errors.Count | Should -Be 0
    }

    It '<_> は Windows 以外では警告を出して終了する（$IsWindows ガード）' -ForEach @(
        'Get-VbScriptScheduledTaskDependency.ps1'
        'Get-VbScriptExecutionLog.ps1'
        'Set-ScriptSignature.ps1'
    ) {
        if ($IsWindows) {
            Set-ItResult -Skipped -Because 'このテストは Windows 以外の環境でガードの動作を確認するものです'
            return
        }

        $result = & "$PSScriptRoot/../src/$_" -WarningVariable warnings -WarningAction SilentlyContinue
        $result | Should -BeNullOrEmpty
        @($warnings).Count | Should -BeGreaterThan 0
    }
}
