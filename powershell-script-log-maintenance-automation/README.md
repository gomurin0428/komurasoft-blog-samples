# PowerShellスクリプト応用 ── ログ調査・アーカイブ・レポート化を安全に自動化する ── サンプルコード

ブログ記事「[PowerShellスクリプト応用 ── ログ調査・アーカイブ・レポート化を安全に自動化する](https://comcomponent.com/blog/2026/06/01/000-powershell-script-log-maintenance-automation/)」のサンプルコードです。

ログ調査・CSV レポート・古いログのアーカイブ（移動）・証跡保存をひとつの運用スクリプト `Invoke-LogMaintenance.ps1` にまとめています。削除は行わず、変更処理は `-WhatIf` / `-Confirm` に対応した関数に分離し、`-Preview` で予行してから本実行する**「見る → 記録する → 予行する → 実行する → 証跡を残す」**という記事の流れをそのまま動かせます。

## 構成

```
powershell-script-log-maintenance-automation/
├── src/
│   ├── Invoke-LogMaintenance.ps1        完成版スクリプト（記事 8 章）
│   ├── log-maintenance.sample.json      設定ファイルの例（記事 4 章）
│   ├── New-SampleLogEnvironment.ps1     一時ディレクトリにダミーログと設定を作る補助スクリプト
│   └── Register-LogMaintenanceTask.ps1  タスクスケジューラ登録（記事 11 章、Windows 専用）
└── tests/
    └── Invoke-LogMaintenance.Tests.ps1  ダミーログで調査・予行・移動・異常系を検証する Pester テスト
```

## 必要環境

- [PowerShell 7](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell) 以降
- テスト実行には [Pester 5](https://pester.dev/)

## 実行方法

まず、補助スクリプトで一時ディレクトリに Logs / Reports / Archive とダミーログ、設定ファイルを作ります（本番フォルダーには触れません）。

```console
pwsh -NoProfile -Command "./src/New-SampleLogEnvironment.ps1"
```

表示された設定ファイルのパスを使って、記事と同じ順番で実行します。

```console
# 1. 読み取りだけ（アーカイブしない）
pwsh -NoProfile -File ./src/Invoke-LogMaintenance.ps1 -ConfigPath <設定ファイルのパス> -SkipArchive

# 2. 移動予定の確認（-WhatIf 扱いで移動しない）
pwsh -NoProfile -File ./src/Invoke-LogMaintenance.ps1 -ConfigPath <設定ファイルのパス> -Preview

# 3. 問題なければ本実行
pwsh -NoProfile -File ./src/Invoke-LogMaintenance.ps1 -ConfigPath <設定ファイルのパス>
```

実行のたびに `OutputPath` の下へ日時付きフォルダーが作られ、`log-hits.csv`・`archive-targets.csv`・`archive-result.csv`・`summary.json`・`transcript.txt` が証跡として残ります。

テスト（一時ディレクトリにダミーログを作り、調査結果の CSV、`-Preview` で移動しないこと、本実行で相対パスを保って移動すること、設定不備で失敗することなどを検証します）:

```console
pwsh -NoProfile -Command "Invoke-Pester -Path ./tests -Output Detailed"
```

## ポイント

- 設定（パス・日数・パターン）は JSON に分け、スクリプト本体を編集せずに条件を変える
- いきなり移動せず、読み取り（`-SkipArchive`）→ 予行（`-Preview`）→ 本実行の順で進める
- 移動のような変更処理は `SupportsShouldProcess` 付きの関数に分け、`Move-Item` の直前で `$PSCmdlet.ShouldProcess()` を判定する
- 削除はせず、日時付きフォルダーへの「移動」に留める（戻し先が分かる）
- CSV・`summary.json`・transcript・`error.txt` で「いつ・どの条件で・何が起きたか」を追えるようにする

## 記事からの調整点

- 記事のコードは Windows のパス例（`C:\App\Logs` など）で説明していますが、スクリプト本体はそのままクロスプラットフォームで動きます。サンプルでは設定ファイルと補助スクリプトで一時ディレクトリ上のパスを使っています
- `summary.json` の `ComputerName` は、Windows 専用の `$env:COMPUTERNAME` から、どの OS でも値が取れる `[System.Environment]::MachineName` に変更しています
- `Export-CsvWithHeader` の `$InputObject` に `[AllowEmptyCollection()]` を追加しています（ヒット 0 件や `-SkipArchive` 時に空配列を渡してもヘッダー行だけの CSV を出力できるようにするため）
- タスクスケジューラ登録（記事 11 章）は Windows 専用のため、`Register-LogMaintenanceTask.ps1` として分離し、Windows 以外では実行できないようにガードしています

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/01/000-powershell-script-log-maintenance-automation/)をご覧ください。
