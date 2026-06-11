BeforeAll {
    $script:ScriptPath = "$PSScriptRoot/../src/Normalize.ps1"
}

Describe 'Normalize.ps1' {
    BeforeEach {
        $script:InputFile = Join-Path $TestDrive 'in.csv'
        $script:OutputFile = Join-Path $TestDrive 'out.csv'

        Set-Content -LiteralPath $script:InputFile -Encoding UTF8 -Value @(
            'Code,Name',
            '1002,Osaka',
            '1001,Tokyo',
            '1003,Nagoya'
        )
    }

    It 'Code 列で昇順に並べ替えた CSV を出力する' {
        & $script:ScriptPath -InputFile $script:InputFile -OutputFile $script:OutputFile

        $rows = @(Import-Csv $script:OutputFile)
        $rows.Count | Should -Be 3
        $rows.Code | Should -Be @('1001', '1002', '1003')
        $rows[0].Name | Should -Be 'Tokyo'
    }

    It '列構成（Code, Name）を維持する' {
        & $script:ScriptPath -InputFile $script:InputFile -OutputFile $script:OutputFile

        $rows = @(Import-Csv $script:OutputFile)
        $rows[0].PSObject.Properties.Name | Should -Be @('Code', 'Name')
    }
}
