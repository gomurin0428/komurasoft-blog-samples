# PesterによるPowerShellのテスト整備 ── サンプルコード

ブログ記事「[PesterによるPowerShellのテスト整備 ── 運用スクリプトを壊しにくくする実務の型](https://comcomponent.com/blog/2026/06/08/000-pester-powershell-test-maintenance/)」のサンプルコードです。

古いログファイルの削除という典型的な運用スクリプトを題材に、「対象を選ぶ関数」と「削除する関数」を分け、Pester v5 でテストする実務の型を実装しています。日付は `-Now` 引数で固定し、ファイル操作は `$TestDrive` に閉じ込め、削除処理は `Mock` と `-WhatIf` で確認します。

このサンプルでは、記事で紹介している**対象選定のテスト**（`$TestDrive`・境界条件・戻り値の形）と、**危険操作のテスト**（`Mock` による `Remove-Item` の呼び出し確認）、および **CI 用の実行スクリプト**を提供します。

## 構成

```
pester-powershell-test-maintenance/
├── src/
│   ├── Get-OldLogFile.ps1           削除対象を選ぶ関数（記事 5 章）
│   └── Remove-OldLogFile.ps1        削除する関数。SupportsShouldProcess 付き（記事 9 章）
├── tests/
│   ├── Get-OldLogFile.Tests.ps1     TestDrive・境界条件・戻り値の形のテスト（記事 5・7・8 章）
│   └── Remove-OldLogFile.Tests.ps1  Mock と -WhatIf による削除処理のテスト（記事 10 章）
└── tools/
    └── Invoke-ProjectTests.ps1      CI 用の実行スクリプト。JUnitXml とカバレッジを出力（記事 14 章）
```

## 必要環境

- [PowerShell 7](https://learn.microsoft.com/ja-jp/powershell/scripting/install/installing-powershell) 以降
- [Pester 5](https://pester.dev/docs/introduction/installation)（`Install-Module -Name Pester -Scope CurrentUser -Force -SkipPublisherCheck`）

## 実行方法

テスト（古い `.log` だけを選ぶこと、期限日ちょうどのファイルを含めないこと、`-WhatIf` で `Remove-Item` を呼ばないことなどを検証します）:

```console
pwsh -NoProfile -Command "Invoke-Pester -Path ./tests -Output Detailed"
```

CI 用スクリプト経由（`test-results.xml` と `coverage.xml` を出力します）:

```console
pwsh -NoProfile -File ./tools/Invoke-ProjectTests.ps1
```

スクリプト本体の動作確認（実フォルダーに対しては、まず `-WhatIf` で予行します）:

```powershell
. ./src/Get-OldLogFile.ps1
. ./src/Remove-OldLogFile.ps1

Get-OldLogFile -Path <ログフォルダー> -Days 30
Remove-OldLogFile -Path <ログフォルダー> -Days 30 -WhatIf
```

## ポイント

- いきなり削除をテストするのではなく、先に「どのファイルが対象として選ばれるか」をテストする
- `Get-Date` を直接使わず `-Now` 引数で日付を固定し、テスト実行日に依存しないようにする
- ファイル操作のテストは Pester の `$TestDrive` に閉じ込め、本物のフォルダーに触らない
- `-lt` か `-le` かのような境界条件（期限日ちょうどのファイル）を 1 つテストに足す
- 後続処理が使うプロパティ名（`FullName`、`Name`、`Length`、`LastWriteTime`）も仕様としてテストする
- 削除関数には `SupportsShouldProcess` を付け、`-WhatIf` で予行できるようにする
- 削除のテストは `Remove-Item` を `Mock` し、「呼ばれるべき場面で意図したパスで呼ばれたか」「`-WhatIf` では呼ばれないか」を確認する
- CI 固有の設定（出力形式・カバレッジ・終了コード）はテストファイルではなく実行用スクリプトにまとめる

なお、記事のコードは Windows のフォルダー（`C:\Logs` など）を例にしていますが、このサンプルはクロスプラットフォームで実行できるよう、テストや CI スクリプト内のパス区切りを `/`（`$PSScriptRoot/../src` など）に調整しています。`Mock` のテストに登場する `C:\Logs\old.log` は実在しないパスで構わないため、記事のまま収録しています。

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/08/000-pester-powershell-test-maintenance/)をご覧ください。
