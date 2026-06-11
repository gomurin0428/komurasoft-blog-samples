BeforeAll {
    $script:ScriptPath = "$PSScriptRoot/../src/Find-VbScriptFileDependency.ps1"
}

Describe 'Find-VbScriptFileDependency' {
    BeforeEach {
        $script:Root = Join-Path $TestDrive 'scan-target'
        New-Item -ItemType Directory -Path $script:Root -Force | Out-Null

        $sub = Join-Path $script:Root 'tools'
        New-Item -ItemType Directory -Path $sub -Force | Out-Null

        # VBScript 本体（WScript.Shell / RegExp / ExecuteGlobal を含む）
        Set-Content -LiteralPath (Join-Path $script:Root 'legacy.vbs') -Encoding UTF8 -Value @(
            'Set sh = CreateObject("WScript.Shell")',
            'Set re = CreateObject("VBScript.RegExp")',
            'ExecuteGlobal code'
        )

        # サブフォルダの .wsf から cscript.exe / .vbs を呼んでいるケース
        Set-Content -LiteralPath (Join-Path $sub 'job.wsf') -Encoding UTF8 -Value @(
            '<job><script language="VBScript">',
            'sh.Run "cscript.exe //nologo cleanup.vbs"',
            '</script></job>'
        )

        # バッチから wscript.exe を起動しているケース
        Set-Content -LiteralPath (Join-Path $script:Root 'run.bat') -Encoding UTF8 -Value `
            'wscript.exe monthly.vbs'

        # 依存のないファイル（検出されないこと）
        Set-Content -LiteralPath (Join-Path $script:Root 'clean.txt') -Encoding UTF8 -Value `
            'no script dependency here'

        # 対象拡張子に含まれないファイル（中身に依存があっても検出されないこと）
        Set-Content -LiteralPath (Join-Path $script:Root 'skipped.log') -Encoding UTF8 -Value `
            'wscript.exe hidden.vbs'

        $script:Csv = Join-Path $TestDrive 'hits.csv'
    }

    It 'ダミーの .vbs / .wsf / .bat から依存パターンを検出する' {
        $hits = & $script:ScriptPath -Paths $script:Root -OutputCsv $script:Csv

        $files = $hits.Path | Split-Path -Leaf | Sort-Object -Unique
        $files | Should -Contain 'legacy.vbs'
        $files | Should -Contain 'job.wsf'
        $files | Should -Contain 'run.bat'
    }

    It '依存のないファイルと対象外拡張子は検出しない' {
        $hits = & $script:ScriptPath -Paths $script:Root -OutputCsv $script:Csv

        $files = $hits.Path | Split-Path -Leaf | Sort-Object -Unique
        $files | Should -Not -Contain 'clean.txt'
        $files | Should -Not -Contain 'skipped.log'
    }

    It 'WScript.Shell と VBScript.RegExp の行を行番号付きで報告する' {
        $hits = & $script:ScriptPath -Paths $script:Root -OutputCsv $script:Csv

        $vbsHits = @($hits | Where-Object { (Split-Path $_.Path -Leaf) -eq 'legacy.vbs' })
        $vbsHits.Line | Should -Contain 'Set sh = CreateObject("WScript.Shell")'
        ($vbsHits | Where-Object Line -Match 'VBScript\.RegExp').LineNumber |
            Should -Contain 2
    }

    It '検出結果を CSV に出力する' {
        & $script:ScriptPath -Paths $script:Root -OutputCsv $script:Csv | Out-Null

        Test-Path $script:Csv | Should -BeTrue
        $rows = @(Import-Csv $script:Csv)
        $rows.Count | Should -BeGreaterThan 0
        $rows[0].PSObject.Properties.Name | Should -Contain 'Path'
        $rows[0].PSObject.Properties.Name | Should -Contain 'LineNumber'
        $rows[0].PSObject.Properties.Name | Should -Contain 'Line'
    }

    It '存在しないパスを指定してもエラーにならない' {
        $missing = Join-Path $TestDrive 'missing'
        { & $script:ScriptPath -Paths $missing -OutputCsv $script:Csv } |
            Should -Not -Throw
    }
}
