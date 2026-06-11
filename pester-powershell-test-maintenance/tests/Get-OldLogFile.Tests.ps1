BeforeAll {
    . "$PSScriptRoot/../src/Get-OldLogFile.ps1"
}

Describe 'Get-OldLogFile' {
    BeforeEach {
        $script:Root = Join-Path $TestDrive 'logs'
        New-Item -ItemType Directory -Path $script:Root -Force | Out-Null

        $oldLog = Join-Path $script:Root 'old.log'
        $newLog = Join-Path $script:Root 'new.log'
        $oldTxt = Join-Path $script:Root 'old.txt'

        Set-Content -LiteralPath $oldLog -Value 'old log' -Encoding UTF8
        Set-Content -LiteralPath $newLog -Value 'new log' -Encoding UTF8
        Set-Content -LiteralPath $oldTxt -Value 'old text' -Encoding UTF8

        (Get-Item -LiteralPath $oldLog).LastWriteTime = [datetime]'2026-05-01T00:00:00'
        (Get-Item -LiteralPath $newLog).LastWriteTime = [datetime]'2026-05-31T00:00:00'
        (Get-Item -LiteralPath $oldTxt).LastWriteTime = [datetime]'2026-05-01T00:00:00'
    }

    It '指定日数より古い .log ファイルだけを返す' {
        $result = Get-OldLogFile `
            -Path $script:Root `
            -Days 30 `
            -Now ([datetime]'2026-06-01T00:00:00')

        $result | Should -HaveCount 1
        $result[0].Name | Should -Be 'old.log'
    }

    It '存在しないフォルダーでは失敗する' {
        { Get-OldLogFile -Path (Join-Path $TestDrive 'missing') } |
            Should -Throw
    }

    It 'ちょうど期限日のファイルは対象にしない' {
        $border = Join-Path $script:Root 'border.log'
        Set-Content -LiteralPath $border -Value 'border log' -Encoding UTF8
        (Get-Item -LiteralPath $border).LastWriteTime = [datetime]'2026-05-02T00:00:00'

        $result = Get-OldLogFile `
            -Path $script:Root `
            -Days 30 `
            -Now ([datetime]'2026-06-01T00:00:00')

        $result.Name | Should -Not -Contain 'border.log'
    }

    It '後続処理で使うプロパティを返す' {
        $result = Get-OldLogFile `
            -Path $script:Root `
            -Days 30 `
            -Now ([datetime]'2026-06-01T00:00:00')

        $propertyNames = $result[0].PSObject.Properties.Name

        $propertyNames | Should -Contain 'FullName'
        $propertyNames | Should -Contain 'Name'
        $propertyNames | Should -Contain 'Length'
        $propertyNames | Should -Contain 'LastWriteTime'
    }
}
