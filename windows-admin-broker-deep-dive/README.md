# Windowsアプリで「管理者権限が必要な処理だけ」を分離する具体的な書き方 ── サンプルコード

ブログ記事「[Windowsアプリで「管理者権限が必要な処理だけ」を分離する具体的な書き方](https://comcomponent.com/blog/2026/03/16/001-windows-admin-broker-deep-dive/)」のサンプルコードです。

Windows の UAC は関数単位ではなくプロセス単位の仕組みのため、「一部の処理だけ管理者で実行」するには実行境界を切る必要があります。このサンプルでは、記事で紹介している **`asInvoker` の UI + `requireAdministrator` の管理者 helper EXE（Administrator Broker Model）** の形を提供します。helper の起動は `ProcessStartInfo` の `Verb = "runas"`、通信は名前付きパイプ、helper が受け付ける操作は固定の allowlist のみです。

## 構成

```
windows-admin-broker-deep-dive/
├── src/KomuraSoft.AdminBroker/          UI と helper で共有するライブラリ（net8.0）
│   ├── BrokerProtocol.cs                共通契約: operation 名・request / response 型・長さプレフィックス付きシリアライザー（記事 9 章）
│   ├── ElevationBrokerClient.cs         UI 側: runas での helper 起動とパイプ通信（記事 10 章）
│   ├── BrokerLaunchOptions.cs           helper 側: 起動引数の厳格な解析（記事 11 章）
│   └── BrokerSession.cs                 helper 側: 要求の読み取り・allowlist dispatch・応答（記事 12 章の dispatch 部分）
├── samples/MyApp/                       UI アプリ相当のコンソールデモ（asInvoker の app.manifest 付き。記事 8.1 章・14 章）
├── samples/MyApp.AdminBroker/           管理者 helper EXE（requireAdministrator の app.manifest 付き。記事 8.2 章）
│   ├── Program.cs                       pipe の明示 ACL 作成と接続元 PID 検証（記事 12 章）
│   └── ExplorerContextMenuRegistration.cs  HKLM への Explorer 右クリックメニュー登録 / 解除（記事 13 章）
└── tests/KomuraSoft.AdminBroker.Tests/  Linux でも実行できるユニットテスト
```

記事の WPF の `SettingsPage.xaml.cs`（記事 14 章）は、このサンプルでは同じ流れのコンソールアプリ（`samples/MyApp/Program.cs`）に置き換えています。

## 必要環境

- Windows（昇格を含む実動作の確認）
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降（ビルドは Linux でも可。samples の csproj の `<EnableWindowsTargeting>true</EnableWindowsTargeting>` により `net8.0-windows` を Linux 上でもビルドできます）

このサンプルのうち、IPC のプロトコル部分（長さプレフィックス付きの型付き要求 / 応答、operation の allowlist dispatch、名前付きパイプでの送受信）は OS 非依存で、作成環境（Linux）でも **実行・テストまで検証済み** です。一方、UAC の昇格プロンプト（`Verb = "runas"`）、`PipeSecurity` による明示 ACL、`GetNamedPipeClientProcessId` による接続元 PID 検証、HKLM のレジストリ操作は Windows 専用のため、**ビルドのみ検証済み** です。実動作は Windows 上で確認してください。

## 実行方法

デモ（非 Windows では runas を行わず、IPC プロトコル部分を同一プロセス内の名前付きパイプで実演します）:

```console
dotnet run --project samples/MyApp
```

Windows では、同じフォルダにある `MyApp.AdminBroker.exe` を `runas` で起動し、昇格プロンプトの承認後に Explorer 右クリックメニューを登録します（`--disable` で解除）。開発レイアウトでは helper の出力先が別フォルダになるため、後述の「Windows での確認手順」のように両方を同じフォルダへ publish してから実行してください。

テスト（フレームの分割読み取り・不正な長さ・途中切断、起動引数の検証、allowlist dispatch、名前付きパイプでの往復、非 Windows での `PlatformNotSupportedException` ガードを検証します。Linux でも実行できます）:

```console
dotnet test
```

## Windows での確認手順

昇格まわりの実動作（Windows 専用部分）は、Windows 上で次のように確認します。

1. UI と helper を同じフォルダへ publish する（helper EXE は UI と同じフォルダから絶対パスで固定解決します）

   ```console
   dotnet publish samples/MyApp -c Release -o publish
   dotnet publish samples/MyApp.AdminBroker -c Release -o publish
   ```

2. 標準ユーザー（または非昇格の管理者）で UI を実行し、UAC の昇格プロンプトが出ることを確認する

   ```console
   .\publish\MyApp.exe
   ```

3. 承認後、レジストリにメニューが登録されていることを確認する

   ```console
   reg query "HKLM\SOFTWARE\Classes\*\shell\MyApp.Open" /s
   ```

   Explorer で任意のファイルを右クリックすると「Open with MyApp」が表示されます。

4. 解除も確認する

   ```console
   .\publish\MyApp.exe --disable
   ```

5. 「失敗すべきときに失敗すること」も確認する（記事 5 章・16 章)
   - 昇格プロンプトをキャンセルすると、設定が変更されずに「承認がキャンセルされました」と表示されること（`ERROR_CANCELLED` = 1223 の扱い。記事 10 章）
   - helper を直接 `MyApp.AdminBroker.exe --pipe x --client-pid 1 --client-sid S-1-5-18` のように起動しても、接続元 PID 検証とタイムアウトにより処理が進まないこと
   - helper に allowlist にない operation を送ると `unsupported_operation` で拒否されること（Linux のテスト / デモでも確認できます）

## ポイント

- 同じプロセスの一部だけを管理者化することはできない。昇格はプロセス境界の話なので、管理者操作は別 EXE（broker）に切り出す
- UI は最後まで `asInvoker`、helper だけ `requireAdministrator`。helper は短命で、固定の allowlist の操作しか受け付けない
- `Verb = "runas"` を使うなら `UseShellExecute = true` を明示する（既定値は .NET Framework と .NET で異なる）
- `runas` と標準入出力リダイレクトは相性が悪いので、通信は名前付きパイプなど別の IPC を使う
- 名前付きパイプは既定 ACL に頼らず、呼び出し元 UI ユーザーの SID にだけ接続権を与える明示的な `PipeSecurity` を組み立てる
- `PipeOptions.CurrentUserOnly` は昇格レベルも確認するため、非昇格 UI ↔ 昇格 helper の通信には使わない
- 接続後に `GetNamedPipeClientProcessId` で想定した UI プロセスの PID か確認する（ただし PID が合っていても要求の再検証は省略しない）
- helper に渡すのは生のコマンド文字列ではなく型付きの要求だけ。操作対象（レジストリパスや登録対象 EXE）は helper 側で固定解決する

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/03/16/001-windows-admin-broker-deep-dive/)をご覧ください。
