# VBScript廃止に備えるVBA・社内ツール点検ガイド ── サンプルコード

ブログ記事「[VBScript廃止に備えるVBA・社内ツール点検ガイド](https://comcomponent.com/blog/2026/04/25/001-vbscript-deprecation-vba-excel-macro-internal-tools-audit-guide/)」のサンプルコードです。

MicrosoftはVBScriptを段階的に廃止する方針を公開しており、いま本当にやるべきことは「全面書き換え」より先に「どこがVBScript依存なのかを可視化すること」です。このサンプルでは、記事で紹介している**棚卸し用のPowerShell監査スクリプト**（ファイル検索・タスクスケジューラ・イベントログ）と、**置換の最小サンプル**（外部 `.vbs` をPowerShellへ寄せるCSV正規化）、および参照用のVBA・Office Scriptsコードを提供します。

## 構成

```
vbscript-deprecation-vba-excel-macro-internal-tools-audit-guide/
├── src/
│   ├── Find-VbScriptFileDependency.ps1          .vbs/.wsf/.hta 等の依存パターンを再帰検索して CSV 化（記事「ファイル検索と構成情報の洗い出し」）
│   ├── Get-VbScriptScheduledTaskDependency.ps1  Scheduled Task から wscript/cscript/mshta・.vbs 呼び出しを抽出（同上、Windows 専用）
│   ├── Get-VbScriptExecutionLog.ps1             Sysmon の vbscript.dll ロードと AppLocker 監査イベントを確認（記事「ログ分析」、Windows 専用）
│   ├── Normalize.ps1                            外部 .vbs を置き換えた CSV 正規化スクリプト（記事「置換の最小サンプル」）
│   ├── Set-ScriptSignature.ps1                  Authenticode 署名の付与と確認（同上、Windows 専用）
│   ├── vba/                                     参照用（Excel 上で使用）
│   │   ├── ScanProjectForVbScriptRisks.bas      VBA プロジェクト内の VBScript 依存スキャン（記事「VBAコード解析」）
│   │   ├── ExportCsvNativeVba.bas               VBA ネイティブでの CSV 出力（記事「置換の最小サンプル」）
│   │   └── RunModernPs.bas                      Excel から Normalize.ps1 を起動する例（同上）
│   └── office-scripts/
│       └── FormatAndSortTable.ts                参照用（Excel の Office Scripts として使用）
└── tests/
    ├── Find-VbScriptFileDependency.Tests.ps1    ダミーの .vbs/.wsf/.bat を作って検出・CSV 出力を検証
    ├── Normalize.Tests.ps1                      Code 列での並べ替えと列構成を検証
    └── WindowsOnlyScripts.Tests.ps1             全 .ps1 の構文解析（パースエラー 0）と $IsWindows ガードの検証
```

## 必要環境

- [PowerShell 7](https://learn.microsoft.com/ja-jp/powershell/scripting/install/installing-powershell) 以降
- [Pester 5](https://pester.dev/docs/introduction/installation)（`Install-Module -Name Pester -Scope CurrentUser -Force -SkipPublisherCheck`）
- 監査スクリプトの実運用（タスクスケジューラ・イベントログ・署名）は Windows 端末上で行います

## 実行方法

テスト（ダミーファイルからの依存検出、CSV 出力、Windows 専用スクリプトの構文・ガードなどを検証します）:

```console
pwsh -NoProfile -Command "Invoke-Pester -Path ./tests -Output Detailed"
```

監査スクリプトの動作確認（Windows 端末上で。対象パスは実環境に合わせて指定します）:

```powershell
# ファイル検索: 指定パス配下の .vbs/.ps1/.bat/.cmd/.wsf/.hta/.txt から依存パターンを検出
./src/Find-VbScriptFileDependency.ps1 -Paths C:\Users, C:\ProgramData, C:\Scripts

# タスクスケジューラ: wscript / cscript / mshta / .vbs を含むタスクを抽出（Windows 専用）
./src/Get-VbScriptScheduledTaskDependency.ps1

# ログ分析: Sysmon の vbscript.dll ロードと AppLocker 監査イベントを確認（Windows 専用）
./src/Get-VbScriptExecutionLog.ps1
```

置換サンプル（CSV 正規化）はクロスプラットフォームで実行できます:

```console
pwsh -NoProfile -File ./src/Normalize.ps1 -InputFile ./in.csv -OutputFile ./out.csv
```

## ポイント

- 棚卸しはファイル名検索だけでは足りない。ファイル検索・タスクスケジューラ・実行ログ（Sysmon / AppLocker / App Control）を別系統で確認する
- `CreateObject` / `Execute` / `ExecuteGlobal` のような文字列ベースの依存は静的検索から漏れやすいため、検索パターンに含める
- タスク定義は `Execute`（wscript / cscript / mshta）と `Arguments`（`.vbs`）の両方を見る
- 「動いている」ログだけでなく、AppLocker / App Control の監査モードで「ブロックされるはずだった」ログも集める
- 外部 `.vbs` の置換先は責務で選ぶ。ブック内で完結するなら VBA ネイティブ / Office Scripts、OS・ファイル・タスクに触るなら PowerShell
- PowerShell へ置き換えた後は、実行ポリシー・Authenticode 署名・ASR / AppLocker / App Control の影響を本番相当条件で再試験する

## Windows 固有・VBA コードの扱い

- `Get-VbScriptScheduledTaskDependency.ps1`・`Get-VbScriptExecutionLog.ps1`・`Set-ScriptSignature.ps1` は Windows 専用 API（`Get-ScheduledTask`、Windows イベントログ、`Cert:` ドライブ）を使うため、`$IsWindows` ガードで Windows 以外では警告を出して終了します。テストでは構文解析（パースエラー 0）とガードの動作を検証しています
- `Find-VbScriptFileDependency.ps1` は記事では `C:\Users` などを対象にしていますが、対象パスを引数化してあり、テストでは一時ディレクトリにダミーの `.vbs` / `.wsf` / `.bat` を作って実際の検出を検証しています
- `src/vba/` の `.bas` と `src/office-scripts/` の `.ts` は参照用です（Excel 上で使用）。`ScanProjectForVbScriptRisks.bas` の実行には、トラスト センターで「VBA プロジェクト オブジェクト モデルへのアクセスを信頼する」の有効化が必要です

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/04/25/001-vbscript-deprecation-vba-excel-macro-internal-tools-audit-guide/)をご覧ください。
