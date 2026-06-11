Describe 'Find-OldLogs.ps1（記事 16 章）' {
    BeforeAll {
        $script:ScriptPath = "$PSScriptRoot/../src/Find-OldLogs.ps1"

        $script:LogRoot = Join-Path $TestDrive 'logs'
        $sub = Join-Path $script:LogRoot 'sub'
        New-Item -ItemType Directory -Path $sub -Force | Out-Null

        'old' | Set-Content (Join-Path $script:LogRoot 'old.log') -Encoding UTF8
        'new' | Set-Content (Join-Path $script:LogRoot 'new.log') -Encoding UTF8
        'old text' | Set-Content (Join-Path $script:LogRoot 'old.txt') -Encoding UTF8
        'nested old' | Set-Content (Join-Path $sub 'nested-old.log') -Encoding UTF8

        (Get-Item (Join-Path $script:LogRoot 'old.log')).LastWriteTime = (Get-Date).AddDays(-60)
        (Get-Item (Join-Path $script:LogRoot 'old.txt')).LastWriteTime = (Get-Date).AddDays(-60)
        (Get-Item (Join-Path $sub 'nested-old.log')).LastWriteTime = (Get-Date).AddDays(-60)

        $script:OutputPath = Join-Path $TestDrive 'old-logs.csv'

        & $script:ScriptPath `
            -Path $script:LogRoot `
            -Days 30 `
            -OutputPath $script:OutputPath *> $null
    }

    It '指定日数より古い .log だけが CSV に出力される（サブフォルダー含む）' {
        $rows = Import-Csv $script:OutputPath

        $rows | Should -HaveCount 2
        ($rows.FullName | Split-Path -Leaf) | Should -Contain 'old.log'
        ($rows.FullName | Split-Path -Leaf) | Should -Contain 'nested-old.log'
    }

    It '新しい .log と .log 以外のファイルは含まれない' {
        $names = Import-Csv $script:OutputPath |
            ForEach-Object { Split-Path $_.FullName -Leaf }

        $names | Should -Not -Contain 'new.log'
        $names | Should -Not -Contain 'old.txt'
    }

    It '後続処理で使う列（FullName / Length / LastWriteTime）を持つ' {
        $rows = Import-Csv $script:OutputPath
        $propertyNames = $rows[0].PSObject.Properties.Name

        $propertyNames | Should -Contain 'FullName'
        $propertyNames | Should -Contain 'Length'
        $propertyNames | Should -Contain 'LastWriteTime'
    }
}
