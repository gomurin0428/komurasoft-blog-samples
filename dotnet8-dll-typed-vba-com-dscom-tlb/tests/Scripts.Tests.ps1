# scripts/ 以下の PowerShell スクリプトの静的検証
#
# TLB 生成や COM 登録そのものは Windows 専用のため、ここでは
# - パースエラーがないこと
# - Windows 以外では $IsWindows ガードにより警告を出して安全に終了すること
# を検証します。
Describe 'Windows 専用スクリプトの静的検証' {
    It '<_> はパースエラーなしで解析できる' -ForEach @(
        'Install-Dscom.ps1'
        'Export-Tlb.ps1'
        'Register-ComServer.ps1'
    ) {
        $tokens = $null
        $errors = $null
        [System.Management.Automation.Language.Parser]::ParseFile(
            "$PSScriptRoot/../scripts/$_", [ref]$tokens, [ref]$errors) | Out-Null

        $errors.Count | Should -Be 0
    }

    It '<_> は Windows 以外では警告を出して終了する（$IsWindows ガード）' -ForEach @(
        'Install-Dscom.ps1'
        'Export-Tlb.ps1'
        'Register-ComServer.ps1'
    ) {
        if ($IsWindows) {
            Set-ItResult -Skipped -Because 'このテストは Windows 以外の環境でガードの動作を確認するものです'
            return
        }

        $result = & "$PSScriptRoot/../scripts/$_" -WarningVariable warnings -WarningAction SilentlyContinue
        $result | Should -BeNullOrEmpty
        @($warnings).Count | Should -BeGreaterThan 0
    }
}
