<#
ログ検索・文字列置換のレシピ集（記事 12・13・33 章）

Select-String による検索（複数パターン・前後行・-SimpleMatch・-CaseSensitive・CSV 化）と、
-replace による安全な置換（別ファイルへのプレビュー出力）をまとめて実行します。

記事のコードは `.\logs\*.log` や `C:\App\Logs\*.log` を対象にしていますが、
このスクリプトは -WorkRoot で指定した作業フォルダーにサンプルログを作って実行します。
#>
[CmdletBinding()]
param(
    # レシピを実行する作業フォルダー。省略時は一時フォルダーに作る
    [string] $WorkRoot = (Join-Path ([IO.Path]::GetTempPath()) ("recipes-logs-" + [guid]::NewGuid().ToString("N")))
)

$ErrorActionPreference = 'Stop'

# ---- サンプルデータの準備（記事には無い、レシピを動かすための下ごしらえ） ----
New-Item -ItemType Directory -Path (Join-Path $WorkRoot 'logs') -Force | Out-Null

Set-Content -LiteralPath (Join-Path $WorkRoot 'logs/app1.log') -Encoding UTF8 -Value @(
    '2026-06-01 09:00:01 INFO  service started'
    '2026-06-01 09:00:05 INFO  connecting to db'
    '2026-06-01 09:00:06 ERROR connection failed'
    '2026-06-01 09:00:07 INFO  retrying'
    '2026-06-01 09:00:10 WARN  slow response'
)
Set-Content -LiteralPath (Join-Path $WorkRoot 'logs/app2.log') -Encoding UTF8 -Value @(
    '2026-06-01 10:00:00 INFO  batch started'
    '2026-06-01 10:00:02 [ERROR] bracket style error'
    '2026-06-01 10:00:03 Error mixed case message'
    '2026-06-01 10:00:04 FATAL batch aborted'
)
Set-Content -LiteralPath (Join-Path $WorkRoot 'appsettings.json') -Encoding UTF8 -Value @'
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=app"
  }
}
'@

Push-Location $WorkRoot
try {
    # ===== 12. ログ検索を少し便利にする ── Select-String =====

    # 文字列検索の基本（33 章「ERROR を含むログ行を CSV にする」のパスを .\logs に調整）
    Select-String -Path .\logs\*.log -Pattern "ERROR"

    # 複数のパターンを指定する
    Select-String -Path .\logs\*.log -Pattern "ERROR", "WARN", "FATAL"

    # 前後の行も見る（33 章「ERROR の前後2行を見る」も同形）
    Select-String -Path .\logs\*.log -Pattern "ERROR" -Context 2

    # 単純な文字列として探す（[ や . を正規表現として解釈させない）
    Select-String -Path .\logs\*.log -Pattern "[ERROR]" -SimpleMatch

    # 大文字・小文字を区別する
    Select-String -Path .\logs\*.log -Pattern "Error" -CaseSensitive

    # 検索結果を CSV に残す
    Select-String -Path .\logs\*.log -Pattern "ERROR" |
      Select-Object Path, LineNumber, Line |
      Export-Csv .\error-lines.csv -NoTypeInformation -Encoding UTF8

    # ===== 13. 文字列を置き換える =====

    # テキストの一部を置き換える
    "server01.example.com" -replace "\.example\.com$", ""

    # 先に Select-String で置換対象を確認する
    Select-String -Path .\appsettings.json -Pattern "localhost" -SimpleMatch

    # いきなり上書きせず、まず別ファイルに出す
    (Get-Content .\appsettings.json -Raw) -replace "localhost", "db01" |
      Set-Content .\appsettings.preview.json -Encoding UTF8
}
finally {
    Pop-Location
}
