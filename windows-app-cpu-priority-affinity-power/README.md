# Windowsアプリ開発者のためのCPU設定入門 ── サンプルコード

ブログ記事「[Windowsアプリ開発者のためのCPU設定入門：優先度・アフィニティ・Pコア/Eコア](https://comcomponent.com/blog/2026/06/02/000-windows-app-cpu-priority-affinity-power/)」のサンプルコードです。

Windows アプリの性能は、コードだけでは決まりません。優先度（いつ実行されやすいか）、アフィニティ（どのCPUで実行してよいか）、Pコア/Eコア、省電力設定、EcoQoS の組み合わせで、実際の応答性や処理時間が決まります。

このサンプルでは、記事で紹介している**優先度・アフィニティの観察と変更**（C# / PowerShell）、**現場での確認コマンド**（powercfg・Performance Counter の PowerShell 関数化）、**EcoQoS の C++ コード**（参照用）と、その検証コードを提供します。

## 構成

```
windows-app-cpu-priority-affinity-power/
├── src/
│   ├── csharp/
│   │   ├── KomuraSoft.CpuSettings/          優先度・アフィニティのライブラリ
│   │   │   ├── ProcessPriorityHelper.cs     優先度クラスの取得・設定（記事 2 章）
│   │   │   ├── TemporaryPriorityScope.cs    一時的に変更して戻すスコープ（記事 12 章）
│   │   │   ├── ProcessorAffinityHelper.cs   アフィニティマスクの組み立て・分解・設定（記事 3 章）
│   │   │   └── CpuConfigurationSnapshot.cs  診断ログ向けスナップショット（記事 14 章）
│   │   └── Demo/                            自プロセスで観察・変更・復元を実演するコンソールアプリ
│   └── powershell/
│       ├── Get-ProcessCpuSetting.ps1        優先度・アフィニティの観察（記事 2・3・10 章）
│       ├── Set-ProcessCpuSetting.ps1        優先度・アフィニティの変更。-WhatIf 対応（記事 2・3 章）
│       └── Get-WindowsPowerInfo.ps1         CPU情報・powercfg・Performance Counter（記事 10 章。Windows 専用）
├── tests/
│   ├── KomuraSoft.CpuSettings.Tests/        xUnit テスト（マスク計算・実プロセスでの設定と復元）
│   └── powershell/                          Pester テスト（観察・変更・Windows 専用部分のガード）
└── docs/
    └── EcoQoS.cpp                           EcoQoS の有効化・無効化（記事 8 章。参照用・ビルド対象外）
```

## 必要環境

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降
- [PowerShell 7](https://learn.microsoft.com/ja-jp/powershell/scripting/install/installing-powershell) 以降
- [Pester 5](https://pester.dev/docs/introduction/installation)（`Install-Module -Name Pester -Scope CurrentUser -Force -SkipPublisherCheck`）

## 実行方法

デモ（自プロセスの優先度・アフィニティを記録し、一時的に変更して戻します）:

```console
dotnet run --project src/csharp/Demo
```

xUnit テスト（マスクの組み立て・分解、実プロセスでのアフィニティ変更と復元、優先度スコープの復元などを検証します）:

```console
dotnet test
```

Pester テスト（観察関数の戻り値の形、-WhatIf で変更しないこと、子プロセスへの優先度・アフィニティ設定、Windows 専用関数のガードなどを検証します）:

```console
pwsh -NoProfile -Command "Invoke-Pester -Path ./tests/powershell -Output Detailed"
```

PowerShell スクリプトの動作確認:

```powershell
. ./src/powershell/Get-ProcessCpuSetting.ps1
. ./src/powershell/Set-ProcessCpuSetting.ps1

# 観察（現在の PowerShell プロセス）
Get-ProcessCpuSetting

# 変更はまず -WhatIf で予行する
Set-ProcessCpuSetting -Id $PID -PriorityClass AboveNormal -AffinityMask 0xF -WhatIf
```

Windows 上では、さらに電源まわりも確認できます:

```powershell
. ./src/powershell/Get-WindowsPowerInfo.ps1

Get-CpuInfo                   # Win32_Processor
Get-ActivePowerScheme         # powercfg /getactivescheme
Get-ProcessorPowerSetting     # powercfg /q SCHEME_CURRENT SUB_PROCESSOR
Get-CpuPerformanceCounter     # % Processor Performance / % Processor Utility
New-PowerEnergyReport -WhatIf # powercfg /energy（管理者権限で実行）
```

## 検証範囲（Linux での実行検証とビルド・構文検証のみの区別）

このサンプルは Linux（Ubuntu 24.04 / .NET 8 / PowerShell 7.4 / Pester 5.7）で作成・検証しています。

**Linux で実行検証した部分**

- C# の `ProcessPriorityHelper` / `TemporaryPriorityScope` / `ProcessorAffinityHelper` / `CpuConfigurationSnapshot` と xUnit テスト一式（`Process.PriorityClass` と `Process.ProcessorAffinity` は Windows と Linux の両方でサポートされるため、実プロセスでの設定・復元まで検証済み）
- PowerShell の `Get-ProcessCpuSetting` / `Set-ProcessCpuSetting` と Pester テスト（子プロセスへの優先度・アフィニティ設定まで検証済み）

**ビルド・構文検証のみの部分（Windows 専用）**

- `Get-WindowsPowerInfo.ps1` の各関数（`powercfg`、`Win32_Processor`、`Processor Information` カウンターは Windows 専用）。`$IsWindows` ガードと構文解析・関数定義の検証のみ Linux で実施
- `docs/EcoQoS.cpp`（`SetThreadInformation` / `ThreadPowerThrottling` を使う Win32 API のコード）。**参照用**として収録しており、このリポジトリではビルドしません。Windows 上の C++ プロジェクトに取り込んで使用してください

## ポイント

- 優先度は「いつ実行されやすいか」の設定であり、CPU を速くする設定ではない（I/O 待ちやロック競合、省電力による低周波数には効かない）
- High / RealTime の常用は避け、重要な処理の直前に上げて終わったら戻す（`TemporaryPriorityScope`）
- アフィニティは「どのCPUで実行してよいか」を強めに制約する設定。検証用には有効だが、最初に触る設定ではない
- アフィニティマスクは CPU 番号の決め打ちではなく、組み立て・分解の関数（`CreateMask` / `GetAllowedCpus`）を通して扱う
- 危険な変更（優先度・アフィニティ・powercfg /energy）は `SupportsShouldProcess` を付け、`-WhatIf` で予行できるようにする
- 顧客環境の切り分けのため、優先度・アフィニティ・論理プロセッサ数・OS を診断ログに残せる形にしておく（`CpuConfigurationSnapshot`）
- EcoQoS は「この処理は省電力でよい」と OS に伝える仕組み。バックグラウンド処理には向くが、UI 操作や周期処理には慎重に

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/02/000-windows-app-cpu-priority-affinity-power/)をご覧ください。
