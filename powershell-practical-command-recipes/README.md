# PowerShell実用コマンド集 ── サンプルコード

ブログ記事「[PowerShell実用コマンド集 ── 日常作業でよく使う小さな機能を増やす](https://comcomponent.com/blog/2026/06/01/002-powershell-practical-command-recipes/)」のサンプルコードです。

件数を数える、種類ごとに集計する、2つの結果を比較する、ログを検索する──といった「よく使う小さな部品」を、1行または数行のコマンドで使えるようにするレシピ集です。記事に登場する多数のコード断片を、章立てに沿った**テーマ別のスクリプト**に整理して収録しています。

各スクリプトはサンプルデータを自前で用意して一連のレシピを実行するため、そのまま流して動きを確認できます。コメントに記事の章番号を添えているので、記事と突き合わせながら読めます。

## 構成

```
powershell-practical-command-recipes/
├── src/
│   ├── file-recipes.ps1            ファイル操作・集計（記事 4〜10・32〜34 章）
│   ├── log-search-recipes.ps1      Select-String と -replace（記事 12・13・33 章）
│   ├── data-recipes.ps1            CSV・JSON・Compare-Object・Get-FileHash（記事 14・15・19・20・33 章）
│   ├── process-job-recipes.ps1     プロセス・Measure-Command・Start-Job・履歴・Get-Error（記事 5・6・23・28〜31 章）
│   ├── output-recipes.ps1          Format 系・Set-Clipboard・Tee-Object・Start-Transcript（記事 11・16〜18 章）
│   ├── environment-recipes.ps1     環境変数・プロファイル（記事 21・22 章）
│   ├── windows-recipes.ps1         Get-Service・Get-WinEvent など Windows 固有（記事 8・14・16・24・25・32・33 章）
│   └── network-web-recipes.ps1     Test-Connection・Invoke-WebRequest など（記事 26・27 章）
└── tests/
    ├── Syntax.Tests.ps1                全スクリプトの構文解析（パースエラー 0 件）
    └── CrossPlatformRecipes.Tests.ps1  一時ディレクトリ上でレシピを実際に実行して検証
```

## 必要環境

- [PowerShell 7](https://learn.microsoft.com/ja-jp/powershell/scripting/install/installing-powershell) 以降
- [Pester 5](https://pester.dev/docs/introduction/installation)（`Install-Module -Name Pester -Scope CurrentUser -Force -SkipPublisherCheck`）
- `windows-recipes.ps1` のレシピ（Get-Service、Get-WinEvent、Restart-Service など）は Windows 専用です。Windows 以外では警告を出して終了します
- `network-web-recipes.ps1` は外部ネットワーク（example.com、api.github.com）への接続が必要です

## 実行方法

各スクリプトは単体で実行できます。サンプルデータは一時フォルダーに作られ、レシピの結果が順に出力されます。

```console
pwsh -NoProfile -File ./src/file-recipes.ps1
pwsh -NoProfile -File ./src/log-search-recipes.ps1
pwsh -NoProfile -File ./src/data-recipes.ps1
```

作業フォルダーを指定して、生成された CSV やテキストを後から見ることもできます。

```console
pwsh -NoProfile -File ./src/file-recipes.ps1 -WorkRoot ./work
```

テスト（一時ディレクトリ上でレシピを実際に実行し、CSV の中身・置換結果・削除対象の絞り込みなどを検証します）:

```console
pwsh -NoProfile -Command "Invoke-Pester -Path tests -Output Detailed"
```

## ポイント

- コマンドは省略形ではなく正式名（`Get-ChildItem`、`Select-String` など）で書く
- 「数えるなら `Measure-Object`」「まとめるなら `Group-Object`」「比べるなら `Compare-Object`」のように、目的とセットで覚える
- `Format-Table` / `Format-List` は画面表示の最後だけに使い、CSV に出すときは `Select-Object` で列を選ぶ
- 置換は元ファイルに直接上書きせず、まず `Select-String` で対象を確認してから別ファイルに出す
- 変更系コマンド（`Remove-Item` など）は「対象を見る → 絞る → 数える → CSV に残す → `-WhatIf` で予行 → 実行」の順で使う（`file-recipes.ps1` の 34 章のレシピがこの流れをそのまま実装しています）

## 記事からの調整点

- 記事のコードは現在のフォルダーや `C:\Work`・`C:\Temp`・`C:\Windows` を対象にしていますが、サンプルでは `-WorkRoot` で指定した作業フォルダー（省略時は一時フォルダー）にサンプルデータを作って実行するように調整しています
- `Stop-Process -Name notepad -WhatIf` は、対象プロセスが無い環境でもエラーにならないよう `-ErrorAction SilentlyContinue` を追加しています
- `Start-Job` のレシピは、スクリプトとして最後まで流せるよう `Wait-Job` で完了を待ってから `Receive-Job` しています
- `Set-Clipboard` は Linux では xclip などの外部ツールが必要なため、使えない環境でもスキップして続行するようにしています（`Get-Service` を使う例は `windows-recipes.ps1` 側に収録）
- 環境を変更する操作（`[Environment]::SetEnvironmentVariable(..., "User")`、プロファイルの作成、実際の `Stop-Process`）はコメントアウトして収録しています
- `$env:PATH -split ";"` は Windows 向けの区切り文字のため、クロスプラットフォームで使える `[IO.Path]::PathSeparator` 版も併記しています

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/01/002-powershell-practical-command-recipes/)をご覧ください。
