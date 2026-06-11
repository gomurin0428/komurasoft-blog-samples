<#
表示・出力・記録のレシピ集（記事 11・16〜18 章）

Format-Table / Format-List の使いどころ、Set-Clipboard、Tee-Object、
Start-Transcript をまとめて実行します。

出力ファイル（CSV・テキスト・transcript）は -WorkRoot で指定した
作業フォルダー（省略時は一時フォルダー）に作ります。
#>
[CmdletBinding()]
param(
    # 出力ファイルを置く作業フォルダー。省略時は一時フォルダーに作る
    [string] $WorkRoot = (Join-Path ([IO.Path]::GetTempPath()) ("recipes-output-" + [guid]::NewGuid().ToString("N")))
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path $WorkRoot -Force | Out-Null

Push-Location $WorkRoot
try {
    # ===== 11. Format 系は画面表示の最後だけに使う =====

    # 画面で見やすくする
    Get-Process |
      Sort-Object -Property WorkingSet -Descending |
      Select-Object -First 10 Name, Id, WorkingSet |
      Format-Table -AutoSize

    # 1件の詳細を見る
    Get-Process -Id $PID |
      Format-List *

    # 避ける: Format 系の結果を CSV に出すと「表示用オブジェクト」が書き込まれてしまう
    # Get-Process |
    #   Format-Table Name, Id |
    #   Export-Csv .\process.csv -NoTypeInformation

    # 正しい: CSV に出すなら Select-Object で列を選ぶ
    Get-Process |
      Select-Object Name, Id |
      Export-Csv .\process.csv -NoTypeInformation -Encoding UTF8

    # ===== 16. クリップボードへ送る ── Set-Clipboard =====
    # Linux ではクリップボード連携に xclip などの外部ツールが必要なため、
    # 使えない環境でも続行できるように try / catch で囲んでいる。
    # 記事の最初の例（Get-Service | ... | Set-Clipboard）は Windows 固有のため
    # windows-recipes.ps1 に収録している
    try {
        # Excel に貼るためにタブ区切りにする
        Get-Process |
          Select-Object Name, Id, CPU |
          ConvertTo-Csv -NoTypeInformation -Delimiter "`t" |
          Set-Clipboard

        # クリップボードの中身を見る
        Get-Clipboard
    }
    catch {
        Write-Warning "クリップボードを使えない環境のためスキップしました: $($_.Exception.Message)"
    }

    # ===== 17. 画面にも出して、ファイルにも残す ── Tee-Object =====

    # 画面で見ながら、同時にファイルにも残す
    Get-Process |
      Sort-Object -Property WorkingSet -Descending |
      Select-Object -First 10 Name, Id, WorkingSet |
      Tee-Object -FilePath .\top-process.txt

    # 後で Excel で開きたいなら Export-Csv の方が扱いやすい
    Get-Process |
      Sort-Object -Property WorkingSet -Descending |
      Select-Object -First 10 Name, Id, WorkingSet |
      Export-Csv .\top-process.csv -NoTypeInformation -Encoding UTF8

    # ===== 18. 作業ログを残す ── Start-Transcript =====

    Start-Transcript -Path .\work-log.txt

    # この間に実行したコマンドや画面出力が記録される。
    # transcript に記録されるのは「画面に出た内容」のため、例として Write-Host で画面に出している
    $count = (Get-ChildItem . -File | Measure-Object).Count
    Write-Host "File count: $count"

    Stop-Transcript
}
finally {
    Pop-Location
}
