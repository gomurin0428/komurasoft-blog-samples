<#
CSV・JSON・比較・ハッシュのレシピ集（記事 14・15・19・20・33 章）

Import-Csv の条件抽出、ConvertFrom-Json による設定値の読み書き、
Compare-Object による作業前後の差分確認、Get-FileHash による同一性確認を
まとめて実行します。

このスクリプトは -WorkRoot で指定した作業フォルダーにサンプルの
CSV・JSON・比較用フォルダー・ダウンロードファイルを作って実行します。
#>
[CmdletBinding()]
param(
    # レシピを実行する作業フォルダー。省略時は一時フォルダーに作る
    [string] $WorkRoot = (Join-Path ([IO.Path]::GetTempPath()) ("recipes-data-" + [guid]::NewGuid().ToString("N")))
)

$ErrorActionPreference = 'Stop'

# ---- サンプルデータの準備（記事には無い、レシピを動かすための下ごしらえ） ----
New-Item -ItemType Directory -Path $WorkRoot -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $WorkRoot 'before') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $WorkRoot 'after') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $WorkRoot 'downloads') -Force | Out-Null

# 記事 19 章の users.csv をそのまま使う
Set-Content -LiteralPath (Join-Path $WorkRoot 'users.csv') -Encoding UTF8 -Value @'
Name,Department,Enabled
Suzuki,Sales,true
Tanaka,Accounting,false
Sato,Sales,true
'@

Set-Content -LiteralPath (Join-Path $WorkRoot 'scores.csv') -Encoding UTF8 -Value @'
Name,Score
Suzuki,92
Tanaka,75
Sato,80
'@

Set-Content -LiteralPath (Join-Path $WorkRoot 'settings.json') -Encoding UTF8 -Value @'
{
  "Database": {
    "Host": "localhost",
    "Port": 5432
  }
}
'@

# 比較用フォルダー。removed.txt は before だけ、added.txt は after だけにある
Set-Content -LiteralPath (Join-Path $WorkRoot 'before/common.txt') -Value 'c' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $WorkRoot 'before/removed.txt') -Value 'r' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $WorkRoot 'after/common.txt') -Value 'c' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $WorkRoot 'after/added.txt') -Value 'a' -Encoding UTF8

# ハッシュ確認用のファイル
Set-Content -LiteralPath (Join-Path $WorkRoot 'installer.exe') -Value 'fake installer body' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $WorkRoot 'downloads/tool-v1.zip') -Value 'tool v1' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $WorkRoot 'downloads/tool-v2.zip') -Value 'tool v2' -Encoding UTF8

Push-Location $WorkRoot
try {
    # ===== 19. CSV を読み込んで、条件で見る =====

    # 読み込む
    Import-Csv .\users.csv

    # 部署が Sales の人だけを見る
    Import-Csv .\users.csv |
      Where-Object { $_.Department -eq "Sales" }

    # 有効なユーザーだけを別 CSV に出す
    Import-Csv .\users.csv |
      Where-Object { $_.Enabled -eq "true" } |
      Export-Csv .\enabled-users.csv -NoTypeInformation -Encoding UTF8

    # CSV の値は文字列なので、数値比較では型変換する
    Import-Csv .\scores.csv |
      Where-Object { [int]$_.Score -ge 80 }

    # ===== 20. JSON の設定値を見る =====

    # -Raw でファイル全体を1つの文字列として読み込む
    $config = Get-Content .\settings.json -Raw | ConvertFrom-Json
    $config

    # 特定の値だけを見る
    $config.Database.Host

    # 値を変更して、別ファイルに保存する
    $config.Database.Host = "db01"
    $config |
      ConvertTo-Json -Depth 10 |
      Set-Content .\settings.updated.json -Encoding UTF8

    # ===== 14. 2つの結果を比較する ── Compare-Object =====

    # フォルダーのファイル名一覧を保存して比較する
    Get-ChildItem .\before -File |
      Select-Object -ExpandProperty Name |
      Set-Content .\before-list.txt -Encoding UTF8

    Get-ChildItem .\after -File |
      Select-Object -ExpandProperty Name |
      Set-Content .\after-list.txt -Encoding UTF8

    Compare-Object `
      -ReferenceObject (Get-Content .\before-list.txt) `
      -DifferenceObject (Get-Content .\after-list.txt)

    # ===== 33. 作業前後のファイル一覧を比較する =====
    # 記事では C:\Work を対象にしている。ここでは before / after フォルダーに調整

    Get-ChildItem .\before -File |
      Select-Object -ExpandProperty Name |
      Set-Content .\before.txt -Encoding UTF8

    Get-ChildItem .\after -File |
      Select-Object -ExpandProperty Name |
      Set-Content .\after.txt -Encoding UTF8

    Compare-Object (Get-Content .\before.txt) (Get-Content .\after.txt)

    # ===== 15. ファイルのハッシュ値を確認する ── Get-FileHash =====

    # 1ファイルのハッシュ値
    Get-FileHash .\installer.exe -Algorithm SHA256

    # フォルダー内のファイルをまとめて確認する
    Get-ChildItem .\downloads -File |
      ForEach-Object { Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256 }

    # CSV に残す
    Get-ChildItem .\downloads -File |
      ForEach-Object { Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256 } |
      Select-Object Path, Hash |
      Export-Csv .\hashes.csv -NoTypeInformation -Encoding UTF8
}
finally {
    Pop-Location
}
