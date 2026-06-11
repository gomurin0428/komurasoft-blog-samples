BeforeDiscovery {
    $script:allScripts = Get-ChildItem -Path "$PSScriptRoot/../src" -Filter '*.ps1' |
        ForEach-Object { @{ Name = $_.Name; Path = $_.FullName } }
}

Describe '構文解析（Windows 専用の項目を含む全スクリプト）' {
    It '<Name> がパースエラーなしで解析できる' -ForEach $allScripts {
        $tokens = $null
        $errors = $null

        [System.Management.Automation.Language.Parser]::ParseFile(
            $Path, [ref] $tokens, [ref] $errors) | Out-Null

        $errors | Should -BeNullOrEmpty
    }
}
