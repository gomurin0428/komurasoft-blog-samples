# Windowsの偽装トークンを正しく扱う ── サンプルコード

ブログ記事「[Windowsの偽装トークンを正しく扱う ── スレッド単位の権限借用と安全な戻し方](https://comcomponent.com/blog/2026/06/09/002-windows-impersonation-token/)」のサンプルコードです。

Windows の偽装は「管理者になる魔法」ではなく、アクセスチェックに使われるセキュリティコンテキストを、主にスレッド単位で切り替える仕組みです。このサンプルでは、記事で紹介している **偽装スコープを小さく閉じ込める** 実装（`WindowsIdentity.RunImpersonated` / `RunImpersonatedAsync`、`LogonUser`、`ImpersonateLoggedOnUser` + `RevertToSelf` の `try` / `finally` パターン）を提供します。

## 構成

```
windows-impersonation-token/
├── src/KomuraSoft.Impersonation/        偽装の実装（クラスライブラリ）
│   ├── NativeMethods.cs                 LogonUser などの P/Invoke 宣言（記事 10 章）
│   ├── Win32ImpersonationScope.cs       ImpersonateLoggedOnUser / RevertToSelf の基本形（記事 8 章・12 章）
│   ├── ImpersonatedFileAccess.cs        RunImpersonated / RunImpersonatedAsync でスコープを閉じる（記事 9 章・10 章・13 章）
│   └── PlatformGuard.cs                 非 Windows での早期失敗ガード
├── samples/Demo/                        Windows 上で実行する前提のコンソールアプリ
└── tests/KomuraSoft.Impersonation.Tests/  Linux でも実行できる範囲のユニットテスト
```

## 必要環境

- Windows（実行）
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降（ビルドは Linux でも可。csproj の `<EnableWindowsTargeting>true</EnableWindowsTargeting>` により `net8.0-windows` を Linux 上でもビルドできます）

このサンプルは Windows 専用 API（`LogonUser`、`WindowsIdentity` 等）を扱うため、作成環境（Linux）では **ビルドとテストのみ検証済み** です。偽装の実動作は Windows 上で確認してください。

## 実行方法

デモ（Windows 上で実行します。非 Windows では説明を出して終了します）:

```console
dotnet run --project samples/Demo
```

引数なしの場合、現在のセキュリティコンテキスト（`Name` / `ImpersonationLevel` / `IsAuthenticated`）の確認と、自分自身のトークンによる `RunImpersonated` スコープの実演を行います。

別ユーザーの資格情報でファイルを読む場合（記事 10 章の `LogonUser` の例）:

```console
dotnet run --project samples/Demo -- <userName> <password> <path> [domain]
```

注意: これは API の形を示すためのデモです。実務ではパスワードをコマンドラインや設定ファイルに平文で置かず、Secret Store 等で管理してください。

テスト（引数検証と、非 Windows での `PlatformNotSupportedException` ガード動作を検証します。Linux でも実行できます）:

```console
dotnet test
```

## Windows での確認手順

偽装の実動作（Windows 専用部分）は、Windows 上で次のように確認します。

1. テスト用のローカルユーザーを作成する（例: `testuser`）

   ```console
   net user testuser <パスワード> /add
   ```

2. そのユーザーだけが読めるファイルを用意する（ACL で他ユーザーの読み取りを拒否しておく）

3. デモを実行し、偽装ユーザーとして読み取れることを確認する

   ```console
   dotnet run --project samples/Demo -- testuser <パスワード> C:\Data\testuser-only.txt
   ```

4. 「失敗すべきときに失敗すること」も確認する（記事 22 章）
   - 権限のないユーザーを指定すると `Access denied`（`UnauthorizedAccessException`）になること
   - 存在しないユーザーやパスワード誤りでは `Win32Exception`（ログオン失敗）になること
   - 失敗時のログやコンソール出力にパスワードが出ていないこと

5. デモの出力で、`RunImpersonated` スコープの外に出ると元のユーザーに戻っていることを確認する

## ポイント

- 偽装は権限昇格ではなく、スレッド単位でアクセスチェックのセキュリティコンテキストを切り替える仕組み
- 偽装スコープは最小化する。必要な I/O だけを `RunImpersonated` / `RunImpersonatedAsync` の中に入れる
- Win32 API で偽装するなら、必ず `try` / `finally` で `RevertToSelf` する。戻せない場合は処理を継続しない
- 偽装が必要な非同期処理は `RunImpersonatedAsync` の中で完了まで `await` する（fire-and-forget しない）
- プライマリトークン（プロセス起動用）と偽装トークン（スレッドのアクセスチェック用）を混同しない
- トークンハンドルは `SafeAccessTokenHandle` と `using` で管理し、必ず閉じる
- 成功ケースだけでなく、拒否されるべきケースが確実に失敗することもテストする

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/09/002-windows-impersonation-token/)をご覧ください。
