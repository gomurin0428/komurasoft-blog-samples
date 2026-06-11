# WPF/WinFormsのasyncとUIスレッドを一枚で整理 ── サンプルコード

ブログ記事「[WPF/WinFormsのasyncとUIスレッドを一枚で整理](https://comcomponent.com/blog/2026/03/12/000-wpf-winforms-ui-thread-async-await-one-sheet/)」のサンプルコードです。

WPF / WinForms で `async` / `await` を使うときに大事なのは、**今どのスレッドで走っているか・`await` の続きがどこへ戻るか・UI へ戻す責任をどこが持つか** の 3 つです。このサンプルでは、記事の典型パターン（plain `await` / `Task.Run` / `ConfigureAwait(false)` / `Dispatcher.InvokeAsync` / `BeginInvoke`）を動く形で収録し、「やってはいけない例」（UI スレッドでの `.Result` / `.Wait()`、UI コードへの `ConfigureAwait(false)`）はコメントで明記したうえで実行されない形（メソッドとしての収録のみ）にしています。

## 構成

```
wpf-winforms-ui-thread-async-await-one-sheet/
├── src/KomuraSoft.UiThreadAsyncAwait/   UI 非依存の汎用ライブラリ
│   └── DocumentRepository.cs            ConfigureAwait(false) が自然なライブラリコード（記事 4.3 章）
├── samples/WpfSample/                   WPF サンプル（net8.0-windows）
│   └── MainWindow.xaml.cs               plain await（4.1 章）、Task.Run（4.2 章）、
│                                        ライブラリ + plain await（4.3 章）、Dispatcher.InvokeAsync（5 章）、
│                                        やってはいけない例（4.3 章・4.4 章、未接続メソッドとして収録）
├── samples/WinFormsSample/              WinForms サンプル（net8.0-windows）
│   └── MainForm.cs                      plain await（4.1 章）、BeginInvoke で UI へ戻す（5 章、
│                                        .NET 9+ の InvokeAsync 版はコメントで収録）
└── tests/KomuraSoft.UiThreadAsyncAwait.Tests/
                                         UI スレッドを模した SynchronizationContext で
                                         「await の戻り先」と「.Result のデッドロック」を再現するテスト
```

## 必要環境

- Windows（UI プロジェクト `WpfSample` / `WinFormsSample` の実行）
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降（ビルドと UI 非依存テストの実行は Linux でも可。csproj の `<EnableWindowsTargeting>true</EnableWindowsTargeting>` により `net8.0-windows` を Linux 上でもビルドできます）

このサンプルは WPF / WinForms を扱うため、作成環境（Linux）では UI プロジェクトは **ビルドのみ検証済み** です。画面の実動作（ボタン操作、UI スレッドへの復帰）は Windows 上で確認してください。UI 非依存のライブラリとユニットテストは Linux でもビルド・実行を検証済みです。

## 実行方法

WPF サンプル（Windows 上で実行します）:

```console
dotnet run --project samples/WpfSample
```

WinForms サンプル（Windows 上で実行します）:

```console
dotnet run --project samples/WinFormsSample
```

テスト（plain `await` がキャプチャしたコンテキストへ戻ること、`ConfigureAwait(false)` が戻りを強制しないこと、UI スレッド相当のスレッドで `.Result` するとデッドロックすること、`DocumentRepository` の改行正規化を検証します。Linux でも実行できます）:

```console
dotnet test
```

## ポイント

- UI イベントハンドラで plain `await` した場合、`await` 後の続きは基本的に UI スレッドへ戻る（余計な `Dispatcher` は要らない）
- `Task.Run` は重い CPU 計算を UI スレッドから外すためのもの。I/O 待ちを包まない
- `ConfigureAwait(false)` は「キャプチャした UI コンテキストへ戻ることを強制しない」という意味。UI コードに機械的に付けない
- UI スレッドで `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` を使わない（継続が UI へ戻れず、デッドロックやフリーズになる）
- UI へ明示的に戻すのは必要な場所でだけ。WPF は `Dispatcher.InvokeAsync`、WinForms は `BeginInvoke`（.NET 9 以降なら `Control.InvokeAsync`）
- ライブラリはデータだけ返し、`Dispatcher` / `Control` を直接握らない。UI 側で marshal する

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/03/12/000-wpf-winforms-ui-thread-async-await-one-sheet/)をご覧ください。
