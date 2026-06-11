Describe '構文解析' {
    It '<Name> がパースエラーなしで解析できる' -ForEach @(
        Get-ChildItem -Path "$PSScriptRoot/../src" -Filter '*.ps1' |
            ForEach-Object { @{ Name = $_.Name; FullName = $_.FullName } }
    ) {
        $tokens = $null
        $parseErrors = $null

        [System.Management.Automation.Language.Parser]::ParseFile(
            $FullName, [ref] $tokens, [ref] $parseErrors) | Out-Null

        $parseErrors | Should -BeNullOrEmpty
    }

    It 'src に 8 本のレシピスクリプトがある' {
        Get-ChildItem -Path "$PSScriptRoot/../src" -Filter '*.ps1' |
            Should -HaveCount 8
    }
}
