# PowerShellコマンドの基本 ── サンプルコード

ブログ記事「[PowerShellコマンドの基本 ── まず覚える操作と安全な使い方](https://comcomponent.com/blog/2026/05/29/powershell-command-basics/)」のサンプルコードです。

PowerShell の基本は「長いコマンドを暗記すること」ではなく、`Get-Command` / `Get-Help` / `Get-Member` で調べ、パイプラインで結果をつなぎ、削除・停止・変更の前に `-WhatIf` で影響を確認することです。

このサンプルでは、記事の章立てに沿った**テーマ別のスクリプト集**を提供します。各スクリプトは一時フォルダーに練習用のワークスペース（ログ・CSV・JSON など）を自動作成し、その中で記事のコマンドを実行するため、手元の環境を汚さずに安全に試せます。

## 構成

```
powershell-command-basics/
├── src/
│   ├── New-SampleWorkspace.ps1            練習用ワークスペースを作る共通ヘルパー
│   ├── 01-get-started.ps1                 オブジェクト出力・バージョン確認・Verb-Noun（記事 2〜4 章）
│   ├── 02-discovery-commands.ps1          Get-Command / Get-Help / Get-Member（記事 5 章）
│   ├── 03-pipeline-basics.ps1             Where-Object / Sort-Object / Select-Object と Format 系（記事 6〜7 章）
│   ├── 04-file-folder-operations.ps1      ファイル・フォルダー操作と -WhatIf（記事 8 章）
│   ├── 05-text-search-write.ps1           Get-Content / Select-String / Set-Content（記事 9 章）
│   ├── 06-csv-json.ps1                    Import-Csv / Export-Csv / ConvertFrom-Json（記事 10〜11 章）
│   ├── 07-process-service-eventlog.ps1    プロセス・サービス・イベントログ（記事 12〜13 章）
│   ├── 08-variables-objects.ps1           変数・配列・スプラッティング・PSCustomObject（記事 14〜15 章）
│   ├── 09-execution-policy-and-errors.ps1 実行ポリシーと try/catch（記事 17〜18 章）
│   ├── 10-safe-change-workflow.ps1        変更系コマンドを安全に実行する順番（記事 19・22 章）
│   ├── Find-OldLogs.ps1                   古いログを一覧化するスクリプト（記事 16 章）
│   └── Export-ErrorReport.ps1             ERROR 行を CSV にまとめるスクリプト（記事 21 章）
└── tests/
    ├── Syntax.Tests.ps1                   全スクリプトの構文解析（Windows 専用の項目を含む）
    ├── DemoScripts.Tests.ps1              テーマ別スクリプトの実行と結果の検証
    ├── Find-OldLogs.Tests.ps1             古い .log だけが CSV に出ることの検証
    └── Export-ErrorReport.Tests.ps1       期間内の ERROR 行だけが出ることの検証
```

## 必要環境

- [PowerShell 7](https://learn.microsoft.com/ja-jp/powershell/scripting/install/installing-powershell) 以降
- [Pester 5](https://pester.dev/docs/introduction/installation)（テストを動かす場合。`Install-Module -Name Pester -Scope CurrentUser -Force -SkipPublisherCheck`）

## 実行方法

テーマ別スクリプト（一時フォルダーにワークスペースを作って実行します）:

```console
pwsh -NoProfile -File ./src/04-file-folder-operations.ps1
```

ワークスペースの場所は `-WorkspacePath` で変更できます:

```console
pwsh -NoProfile -File ./src/10-safe-change-workflow.ps1 -WorkspacePath /tmp/ps-practice
```

記事 16・21 章のスクリプト:

```console
pwsh -NoProfile -File ./src/Find-OldLogs.ps1 -Path <ログフォルダー> -Days 60 -OutputPath ./old-logs.csv
pwsh -NoProfile -File ./src/Export-ErrorReport.ps1 -LogPath <ログフォルダー> -Days 14 -Pattern "ERROR|FATAL" -OutputPath ./errors.csv
```

テスト（古い `.log` だけが選ばれること、`-WhatIf` 後の削除・移動が意図どおりであること、全スクリプトが構文エラーなしであることなどを検証します）:

```console
pwsh -NoProfile -Command "Invoke-Pester -Path tests -Output Detailed"
```

## ポイント

- コマンドの暗記より「調べ方」を覚える（`Get-Command` → `Get-Help -Examples` → `Get-Member`）
- 出力は文字列ではなくオブジェクト。何で絞ればよいか迷ったら `Get-Member` でプロパティ名を確認する
- `Where-Object` → `Sort-Object` → `Select-Object` → `Export-Csv` の順でパイプラインをつなぐ
- `Format-Table` などの Format 系は「最後に画面へ見せるため」。`Export-Csv` の前には入れない
- 削除・移動・停止は「見る → 絞る → 記録する → 予行する（`-WhatIf`）→ 実行する」の順番を守る
- 削除前の対象一覧を `Export-Csv` で証跡として残す
- 重要な処理は `-ErrorAction Stop` と `try/catch` で失敗を記録する

## 記事からの調整点

- 記事の `C:\Logs`、`C:\Temp`、`C:\Work\Reports` などの Windows のパスは、クロスプラットフォームで実行できるよう、スクリプトが自動作成する一時ワークスペース内のフォルダー（`logs` / `temp` / `reports`）と `/` 区切りの相対パスに調整しています（調整箇所はコメントで明示）
- `Get-Service` / `Restart-Service` / `Get-WinEvent` などの Windows 専用コマンドレットは `$IsWindows` ガードの中に置き、Windows 以外ではスキップされるようにしています（記事のコード自体はそのまま収録し、テストでは構文解析で検証）
- `Get-Content -Wait`、`Get-Help -Online`、`Update-Help`、`Set-ExecutionPolicy` のような「戻らない・ブラウザーを開く・環境設定を変える」コマンドは、理由を添えたコメントとして収録しています
- `Stop-Process -Name notepad` と `Get-Service -Name "Spooler"` は、対象が存在しない環境でエラーにならないよう存在確認のガードを追加しています

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/05/29/powershell-command-basics/)をご覧ください。
