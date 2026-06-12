# komurasoft-blog-samples

[コムコンポーネント（KomuraSoft）のブログ](https://comcomponent.com/blog/)で紹介しているサンプルコードを置くリポジトリです。

各フォルダが 1 つのブログ記事に対応しています。フォルダ直下の README に、対応する記事へのリンク・構成・実行方法・検証状態を記載しています。詳しい解説は各記事本文をご覧ください。

## サンプル一覧

### C# / .NET

| フォルダ | 記事 |
| --- | --- |
| [tcp-send-receive-message-framing](./tcp-send-receive-message-framing/) | [TCPでSendした単位ごとにReceiveできるという誤解](https://comcomponent.com/blog/2026/06/09/001-tcp-send-receive-message-framing/) |
| [csharp-async-await-best-practices](./csharp-async-await-best-practices/) | [C# async/await実務判断表](https://comcomponent.com/blog/2026/03/09/001-csharp-async-await-best-practices/) |
| [csharp-run-powershell-receive-objects](./csharp-run-powershell-receive-objects/) | [C#でPowerShellを実行して、オブジェクトとして受け取る方法](https://comcomponent.com/blog/2026/06/08/001-csharp-run-powershell-receive-objects/) |
| [dotnet-algebraic-data-types](./dotnet-algebraic-data-types/) | [代数的データ型を.NET Framework / .NETで使う](https://comcomponent.com/blog/2026/06/09/003-dotnet-algebraic-data-types/) |
| [dotnet-gc-or-memory-leak](./dotnet-gc-or-memory-leak/) | [.NETでGC待ちとメモリリークを見分ける](https://comcomponent.com/blog/2026/06/09/000-dotnet-gc-or-memory-leak/) |
| [filesystemwatcher-safe-basics](./filesystemwatcher-safe-basics/) | [FileSystemWatcher実務ガイド](https://comcomponent.com/blog/2026/03/10/000-filesystemwatcher-safe-basics/) |
| [file-integration-locking-best-practices-komurasoft-style](./file-integration-locking-best-practices-komurasoft-style/) | [ファイル連携の排他制御の基礎知識](https://comcomponent.com/blog/2026/03/07/001-file-integration-locking-best-practices-komurasoft-style/) |
| [generic-host-backgroundservice-desktop-app](./generic-host-backgroundservice-desktop-app/) | [.NET Generic HostとBackgroundServiceをデスクトップアプリで使う理由](https://comcomponent.com/blog/2026/03/12/002-generic-host-backgroundservice-desktop-app/) |
| [periodictimer-system-threading-timer-dispatchertimer-guide](./periodictimer-system-threading-timer-dispatchertimer-guide/) | [.NETタイマー3種の使い分け](https://comcomponent.com/blog/2026/03/12/002-periodictimer-system-threading-timer-dispatchertimer-guide/) |
| [roslyn-dotnet-compiler-platform](./roslyn-dotnet-compiler-platform/) | [Roslynとは何か](https://comcomponent.com/blog/2026/06/10/002-roslyn-dotnet-compiler-platform/) |
| [wpf-winforms-ui-thread-async-await-one-sheet](./wpf-winforms-ui-thread-async-await-one-sheet/) | [WPF/WinFormsのasyncとUIスレッドを一枚で整理](https://comcomponent.com/blog/2026/03/12/000-wpf-winforms-ui-thread-async-await-one-sheet/) |
| [csharp-native-aot-native-dll-from-c-cpp](./csharp-native-aot-native-dll-from-c-cpp/) | [C# Native AOT DLLをC/C++から呼び出す方法](https://comcomponent.com/blog/2026/03/12/003-csharp-native-aot-native-dll-from-c-cpp/) |
| [dotnet8-dll-typed-vba-com-dscom-tlb](./dotnet8-dll-typed-vba-com-dscom-tlb/) | [.NET 8 DLLをVBAから型付きで使う方法](https://comcomponent.com/blog/2026/03/16/007-dotnet8-dll-typed-vba-com-dscom-tlb/) |
| [windows-impersonation-token](./windows-impersonation-token/) | [Windowsの偽装トークンを正しく扱う](https://comcomponent.com/blog/2026/06/09/002-windows-impersonation-token/) |
| [windows-admin-broker-deep-dive](./windows-admin-broker-deep-dive/) | [Windowsアプリで「管理者権限が必要な処理だけ」を分離する具体的な書き方](https://comcomponent.com/blog/2026/03/16/001-windows-admin-broker-deep-dive/) |
| [windows-app-cpu-priority-affinity-power](./windows-app-cpu-priority-affinity-power/) | [Windowsアプリ開発者のためのCPU設定入門](https://comcomponent.com/blog/2026/06/02/000-windows-app-cpu-priority-affinity-power/) |

### PowerShell

| フォルダ | 記事 |
| --- | --- |
| [powershell-command-basics](./powershell-command-basics/) | [PowerShellコマンドの基本](https://comcomponent.com/blog/2026/05/29/powershell-command-basics/) |
| [powershell-practical-command-recipes](./powershell-practical-command-recipes/) | [PowerShell実用コマンド集](https://comcomponent.com/blog/2026/06/01/002-powershell-practical-command-recipes/) |
| [powershell-script-log-maintenance-automation](./powershell-script-log-maintenance-automation/) | [PowerShellスクリプト応用 ── ログ調査・アーカイブ・レポート化を安全に自動化する](https://comcomponent.com/blog/2026/06/01/000-powershell-script-log-maintenance-automation/) |
| [pester-powershell-test-maintenance](./pester-powershell-test-maintenance/) | [PesterによるPowerShellのテスト整備](https://comcomponent.com/blog/2026/06/08/000-pester-powershell-test-maintenance/) |
| [vbscript-deprecation-vba-excel-macro-internal-tools-audit-guide](./vbscript-deprecation-vba-excel-macro-internal-tools-audit-guide/) | [VBScript廃止に備えるVBA・社内ツール点検ガイド](https://comcomponent.com/blog/2026/04/25/001-vbscript-deprecation-vba-excel-macro-internal-tools-audit-guide/) |

### Windows ネイティブ（C++）

| フォルダ | 記事 |
| --- | --- |
| [media-foundation-extract-still-image-from-mp4-at-specific-time](./media-foundation-extract-still-image-from-mp4-at-specific-time/) | [Media FoundationでMP4の指定時刻から静止画を切り出す方法](https://comcomponent.com/blog/2026/03/15/000-media-foundation-extract-still-image-from-mp4-at-specific-time/) |
| [media-foundation-yuv-to-rgb-conversion-patterns](./media-foundation-yuv-to-rgb-conversion-patterns/) | [Media FoundationでYUVをRGBに変換する方法](https://comcomponent.com/blog/2026/03/15/002-media-foundation-yuv-to-rgb-conversion-patterns/) |
| [media-foundation-overlay-image-text-on-mp4-frames](./media-foundation-overlay-image-text-on-mp4-frames/) | [Media FoundationでMP4に画像と文字を焼き込む方法](https://comcomponent.com/blog/2026/03/16/009-media-foundation-overlay-image-text-on-mp4-frames/) |
| [cpp-cli-wrapper-for-native-dlls](./cpp-cli-wrapper-for-native-dlls/) | [C#からネイティブDLLを呼ぶ：C++/CLIラッパー vs P/Invoke](https://comcomponent.com/blog/2026/03/07/000-cpp-cli-wrapper-for-native-dlls/) |
| [windows-mfc-overview](./windows-mfc-overview/) | [WindowsのMFCとは何か](https://comcomponent.com/blog/2026/06/11/000-windows-mfc-overview/) |

### Ada

| フォルダ | 記事 |
| --- | --- |
| [ada-language-appeal](./ada-language-appeal/) | [Ada言語の魅力](https://comcomponent.com/blog/ada-language-appeal/) |

## ライセンス

[MIT License](./LICENSE)
