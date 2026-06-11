<#
環境変数・プロファイルのレシピ集（記事 21・22 章）

環境変数の確認とセッション内での設定、プロファイルの場所確認をまとめて実行します。
読み取り中心のため作業フォルダーは使いません。

環境を変更する操作（ユーザー環境変数の永続設定、プロファイルの作成）は
コメントアウトして収録しています。試す場合は影響範囲を確認してから実行してください。
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# ===== 21. 環境変数を見る =====

# 一覧を見る
Get-ChildItem Env:

# 現在のユーザー名を見る（Linux / macOS では USERNAME が無く、空のことがある）
$env:USERNAME

# 一時フォルダーを見る（Linux / macOS では TEMP が無く、空のことがある）
$env:TEMP

# PATH を見やすく分割する（記事は Windows の区切り文字 ";" を使っている）
$env:PATH -split ";"

# クロスプラットフォームで動かす場合は、OS ごとの区切り文字を使う（記事に無い調整）
$env:PATH -split [IO.Path]::PathSeparator

# 現在の PowerShell セッションだけで環境変数を設定する
$env:APP_MODE = "Development"

# ユーザー環境変数として保存する場合（環境を変更する操作のため、ここでは実行しない）
# 会社 PC や本番サーバーでは、影響範囲を確認してから行う
# [Environment]::SetEnvironmentVariable("APP_MODE", "Development", "User")

# ===== 22. プロファイルを確認する =====

# 場所を確認する
$PROFILE

# 存在するか確認する
Test-Path $PROFILE

# 作成する場合（環境を変更する操作のため、ここでは実行しない）
# New-Item -ItemType File -Path $PROFILE -Force

# メモ帳で開く場合（Windows 向け。ここでは実行しない）
# notepad $PROFILE
