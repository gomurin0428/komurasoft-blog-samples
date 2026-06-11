#Requires -Version 7.0
<#
    記事「PowerShellコマンドの基本」 9 章のコード集。
    テキストファイルを読む（Get-Content）、探す（Select-String）、
    書く（Set-Content / Add-Content）を確認します。

    実行例:
        pwsh -NoProfile -File ./src/05-text-search-write.ps1
#>
[CmdletBinding()]
param(
    [string] $WorkspacePath = (Join-Path ([System.IO.Path]::GetTempPath()) 'powershell-command-basics/05-text')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/New-SampleWorkspace.ps1"
New-SampleWorkspace -Path $WorkspacePath | Out-Null

Push-Location $WorkspacePath
try {
    # --- ファイルを読む ---

    Get-Content ./logs/app.log

    # 末尾だけ見る場合は -Tail を使う
    Get-Content ./logs/app.log -Tail 50

    # 追記されるログを眺める場合は -Wait を使う
    # （追記を待ち続けて戻らないため、ここではコメントにしている）
    # Get-Content ./logs/app.log -Tail 20 -Wait

    # --- 文字列を検索する ---

    Select-String -Path ./logs/app.log -Pattern "ERROR"

    # 複数ファイル・複数パターンを対象にできる
    Select-String -Path ./logs/*.log -Pattern "ERROR", "WARN"

    # 検索結果から、ファイル名・行番号・内容を取り出して CSV にする
    Select-String -Path ./logs/*.log -Pattern "ERROR" |
      Select-Object Path, LineNumber, Line |
      Export-Csv ./error-lines.csv -NoTypeInformation -Encoding UTF8

    # --- ファイルへ書き込む ---

    "hello" | Set-Content ./memo.txt -Encoding UTF8
    "next line" | Add-Content ./memo.txt -Encoding UTF8
    Get-Content ./memo.txt

    # コマンド結果を保存する場合は、Out-File より
    # Export-Csv や ConvertTo-Json の方が後で扱いやすいことがある
    Get-Process |
      Select-Object Name, Id, CPU |
      Export-Csv ./process.csv -NoTypeInformation -Encoding UTF8
}
finally {
    Pop-Location
}
