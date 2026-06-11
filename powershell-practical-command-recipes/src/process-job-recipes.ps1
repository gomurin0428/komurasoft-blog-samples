<#
プロセス・時間計測・ジョブ・履歴・エラーのレシピ集（記事 5・6・23・28〜31 章）

Get-Process の集計・絞り込み、Measure-Command による時間計測、
Start-Job によるバックグラウンド実行、Get-History、Get-Error をまとめて実行します。

記事の Measure-Command / Start-Job は C:\Windows を走査していますが、
このスクリプトは -WorkRoot で指定した作業フォルダー（省略時は一時フォルダー）を
走査対象に調整しています。
#>
[CmdletBinding()]
param(
    # 走査対象にする作業フォルダー。省略時は一時フォルダーに作る
    [string] $WorkRoot = (Join-Path ([IO.Path]::GetTempPath()) ("recipes-process-" + [guid]::NewGuid().ToString("N")))
)

$ErrorActionPreference = 'Stop'

# ---- サンプルデータの準備（記事には無い、走査対象の下ごしらえ） ----
New-Item -ItemType Directory -Path $WorkRoot -Force | Out-Null
1..20 | ForEach-Object {
    Set-Content -LiteralPath (Join-Path $WorkRoot ("file{0:d2}.txt" -f $_)) -Value "sample $_" -Encoding UTF8
}

Push-Location $WorkRoot
try {
    # ===== 6. 値だけ取り出す ── Select-Object -ExpandProperty =====

    # 列を選ぶ（Name、Id、CPU という列を持ったオブジェクトを出す）
    Get-Process |
      Select-Object Name, Id, CPU

    # 値だけを取り出す（notepad が動いていなければ何も出ない）
    Get-Process -Name notepad -ErrorAction SilentlyContinue |
      Select-Object -ExpandProperty Id

    # ===== 5. 最大・平均も調べる（プロセスの累積 CPU 時間） =====
    # CPU は使用率ではなく、プロセス起動以降に使った CPU 時間の秒数。
    # 値を取れないプロセスが混ざることがあるため -ErrorAction SilentlyContinue を追加している
    Get-Process |
      Measure-Object -Property CPU -Sum -ErrorAction SilentlyContinue

    # ===== 23. プロセスを見る =====

    # メモリ使用量の大きい順に見る（9 章の計算列、33 章「メモリ使用量の大きいプロセスを見る」も同形）
    Get-Process |
      Sort-Object -Property WorkingSet -Descending |
      Select-Object -First 10 Name, Id, @{Name="MemoryMB"; Expression={ [math]::Round($_.WorkingSet / 1MB, 1) }}

    # 特定の名前で絞る
    Get-Process -Name notepad -ErrorAction SilentlyContinue

    # 停止する前に -WhatIf で確認する。
    # 記事のまま実行すると対象プロセスが無い環境ではエラーになるため -ErrorAction SilentlyContinue を追加している
    Stop-Process -Name notepad -WhatIf -ErrorAction SilentlyContinue

    # 問題なければ実行する（このサンプルでは実行しない）
    # Stop-Process -Name notepad

    # ===== 28. 実行時間を測る ── Measure-Command =====
    # 記事では C:\Windows を走査している。ここでは作業フォルダーに調整

    Measure-Command {
      Get-ChildItem . -File -Recurse -ErrorAction SilentlyContinue |
        Measure-Object
    }

    $result = Measure-Command {
      Get-ChildItem . -File -Recurse | Measure-Object
    }

    $result.TotalSeconds

    # ===== 29. 時間のかかる処理を裏で動かす ── Start-Job =====
    # 記事では C:\Windows を走査している。ここでは作業フォルダーに調整
    # （ジョブは別プロセスで動くため、$using: でフォルダーのパスを渡している）

    $job = Start-Job -ScriptBlock {
      Get-ChildItem $using:WorkRoot -File -Recurse -ErrorAction SilentlyContinue |
        Measure-Object
    }

    # ジョブの状態を見る
    Get-Job

    # サンプルを最後まで流せるように、結果を受け取る前に完了を待つ（記事に無い調整）
    Wait-Job $job | Out-Null

    # 結果を受け取る
    Receive-Job $job

    # 不要になったジョブを削除する
    Remove-Job $job

    # ===== 30. コマンド履歴を使う =====
    # スクリプトとして実行した場合、履歴は空のことが多い（対話セッションで試すレシピ）

    Get-History

    Get-History |
      Select-Object Id, CommandLine

    # 特定の履歴を再実行する（対話セッションで内容を確認してから使う）
    # Invoke-History 12

    # ===== 31. エラーの詳細を見る ── Get-Error =====

    # 動作確認用に、わざとエラーを1件起こしておく（記事には無い下ごしらえ）
    Get-Item .\no-such-file.txt -ErrorAction SilentlyContinue

    # 直近のエラーを詳しく見る
    Get-Error

    # 直近のエラーメッセージだけ見る
    $Error[0].Exception.Message
}
finally {
    Pop-Location
}
