<#
ファイル操作・集計のレシピ集（記事 4〜10・32〜34 章）

件数・合計サイズ・重複整理・拡張子別集計・計算列・並べ替え・
変更系コマンドの安全な流れ（対象確認 → CSV → -WhatIf → 実行）をまとめて実行します。

記事のコードは現在のフォルダー（`.`）や `C:\Work`・`C:\Temp` を対象にしていますが、
このスクリプトは -WorkRoot で指定した作業フォルダー（省略時は一時フォルダー）に
サンプルファイルを作り、その中で各レシピを実行します。
#>
[CmdletBinding()]
param(
    # レシピを実行する作業フォルダー。省略時は一時フォルダーに作る
    [string] $WorkRoot = (Join-Path ([IO.Path]::GetTempPath()) ("recipes-files-" + [guid]::NewGuid().ToString("N")))
)

$ErrorActionPreference = 'Stop'

# ---- サンプルデータの準備（記事には無い、レシピを動かすための下ごしらえ） ----
New-Item -ItemType Directory -Path $WorkRoot -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $WorkRoot 'sub') -Force | Out-Null

Set-Content -LiteralPath (Join-Path $WorkRoot 'report.txt') -Value 'report body' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $WorkRoot 'app.log') -Value 'INFO started' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $WorkRoot 'old.log') -Value 'INFO old run' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $WorkRoot 'data.csv') -Value 'a,b,c' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $WorkRoot 'sub/notes.txt') -Value 'notes' -Encoding UTF8
[IO.File]::WriteAllBytes((Join-Path $WorkRoot 'big.bin'), (New-Object byte[] 2MB))

# 削除レシピ（記事 34 章）用の .tmp ファイル。2 つは 40 日前、1 つは今日の日付にする
Set-Content -LiteralPath (Join-Path $WorkRoot 'old1.tmp') -Value 'tmp1' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $WorkRoot 'old2.tmp') -Value 'tmp2' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $WorkRoot 'new.tmp') -Value 'tmp3' -Encoding UTF8
(Get-Item -LiteralPath (Join-Path $WorkRoot 'old1.tmp')).LastWriteTime = (Get-Date).AddDays(-40)
(Get-Item -LiteralPath (Join-Path $WorkRoot 'old2.tmp')).LastWriteTime = (Get-Date).AddDays(-40)
(Get-Item -LiteralPath (Join-Path $WorkRoot 'old.log')).LastWriteTime = (Get-Date).AddDays(-10)

Push-Location $WorkRoot
try {
    # ===== 4. 件数を数える ── Measure-Object =====

    # 現在のフォルダーにあるファイル数を数える
    Get-ChildItem . -File | Measure-Object

    # サブフォルダーも含めて数える
    Get-ChildItem . -File -Recurse | Measure-Object

    # ファイルサイズの合計（バイト数）
    Get-ChildItem . -File -Recurse |
      Measure-Object -Property Length -Sum

    # GB 単位に直す
    $size = Get-ChildItem . -File -Recurse |
      Measure-Object -Property Length -Sum

    [math]::Round($size.Sum / 1GB, 2)

    # ===== 5. 最大・平均も調べる =====

    Get-ChildItem . -File |
      Measure-Object -Property Length -Sum -Average -Maximum -Minimum

    # ===== 6. 値だけ取り出す ── Select-Object -ExpandProperty =====

    # ファイル名だけを取り出す
    Get-ChildItem . -File |
      Select-Object -ExpandProperty Name

    # ファイル名一覧だけを保存する
    Get-ChildItem . -File |
      Select-Object -ExpandProperty Name |
      Set-Content .\file-names.txt -Encoding UTF8

    # ===== 7. 重複を消す ── Sort-Object -Unique =====

    # ファイルの拡張子だけを一覧にする
    Get-ChildItem . -File |
      Select-Object -ExpandProperty Extension |
      Sort-Object -Unique

    # ===== 8. 種類ごとに集計する ── Group-Object =====

    # 拡張子ごとにファイル数を数える（33 章「拡張子ごとのファイル数を見る」も同形）
    Get-ChildItem . -File |
      Group-Object -Property Extension -NoElement |
      Sort-Object -Property Count -Descending

    # ===== 9. 必要な列を作る ── 計算した列を追加する =====

    # KB に直した列を作る
    Get-ChildItem . -File |
      Select-Object Name, @{Name="SizeKB"; Expression={ [math]::Round($_.Length / 1KB, 1) }}, LastWriteTime

    # サブフォルダーも含めて、大きいファイル順に見る（33 章「大きいファイル上位20件を見る」も同形）
    Get-ChildItem . -File -Recurse |
      Sort-Object -Property Length -Descending |
      Select-Object -First 20 FullName, @{Name="SizeMB"; Expression={ [math]::Round($_.Length / 1MB, 2) }}, LastWriteTime

    # ===== 10. 新しい順・古い順・大きい順に見る =====

    # 最近更新されたファイル
    Get-ChildItem . -File |
      Sort-Object -Property LastWriteTime -Descending |
      Select-Object -First 10 Name, LastWriteTime

    # 古いファイル
    Get-ChildItem . -File |
      Sort-Object -Property LastWriteTime |
      Select-Object -First 10 Name, LastWriteTime

    # 大きいファイル
    Get-ChildItem . -File -Recurse |
      Sort-Object -Property Length -Descending |
      Select-Object -First 10 FullName, Length

    # ===== 32. よく使う比較演算子 =====

    # 10MB より大きいファイル（このサンプルデータでは該当なし）
    Get-ChildItem . -File |
      Where-Object { $_.Length -gt 10MB }

    # 名前が .log で終わるファイル
    Get-ChildItem . -File |
      Where-Object { $_.Name -match "\.log$" }

    # ===== 33. ちょっとした実務レシピ（ファイル編） =====
    # 記事では C:\Work を対象にしている。ここでは作業フォルダー（カレント）に調整

    # 直近24時間に更新されたファイルを見る
    $since = (Get-Date).AddHours(-24)

    Get-ChildItem . -File -Recurse |
      Where-Object { $_.LastWriteTime -ge $since } |
      Select-Object FullName, Length, LastWriteTime

    # フォルダーの合計サイズを GB で見る
    $size = Get-ChildItem . -File -Recurse |
      Measure-Object -Property Length -Sum

    [math]::Round($size.Sum / 1GB, 2)

    # ===== 34. 変更系コマンドと組み合わせるときの注意 =====
    # 古い .tmp ファイルを削除する流れ。記事では C:\Temp を対象にしている

    # 1. まず対象を見る
    $limit = (Get-Date).AddDays(-30)

    $targets = Get-ChildItem . -Filter *.tmp -File -Recurse |
      Where-Object { $_.LastWriteTime -lt $limit }

    $targets |
      Select-Object FullName, Length, LastWriteTime

    # 2. 件数を数える
    $targets | Measure-Object

    # 3. CSV に残す
    $targets |
      Select-Object FullName, Length, LastWriteTime |
      Export-Csv .\delete-targets.csv -NoTypeInformation -Encoding UTF8

    # 4. 削除予定を確認する
    $targets | Remove-Item -WhatIf

    # 5. 問題なければ実行する（このサンプルでは作業フォルダー内の .tmp だけが対象）
    $targets | Remove-Item
}
finally {
    Pop-Location
}
