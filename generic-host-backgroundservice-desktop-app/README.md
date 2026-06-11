# .NET Generic HostとBackgroundServiceをデスクトップアプリで使う理由 ── サンプルコード

ブログ記事「[.NET Generic HostとBackgroundServiceをデスクトップアプリで使う理由](https://comcomponent.com/blog/2026/03/12/002-generic-host-backgroundservice-desktop-app/)」のサンプルコードです。

Windows ツールや常駐アプリでは、定期ポーリング・再接続・キュー処理のような「アプリ全体の寿命にぶら下がる処理」が UI コードのついでに増えていきがちです。Generic Host と `BackgroundService` を使うと、起動責務 / 停止責務 / 例外監視 / ログ / DI / 設定を 1 か所の設計に寄せ、graceful shutdown を入口から扱えます。

このサンプルでは、記事 5 章の最小構成例（5 秒ごとに外部状態を読む `BackgroundService` と、UI と worker の間に置く薄い共有層 `StatusStore`）の実装と、その検証コードを提供します。

## 構成

```
generic-host-backgroundservice-desktop-app/
├── src/KomuraSoft.DesktopHostSample/             ホストに載せる常駐処理（クラスライブラリ）
│   ├── DevicePollingBackgroundService.cs         StartAsync / ExecuteAsync / StopAsync の分け方（記事 5.2 / 6 章）
│   ├── StatusStore.cs                            UI 直結にしない状態共有層と DeviceStatus（記事 5.3）
│   ├── IDeviceStatusReader.cs                    scoped な読み取りサービスのインターフェイス
│   └── DeviceStatusReader.cs                     装置 I/O を模擬する実装（記事ではコード未掲載のため補完）
├── samples/Demo/                                 起動 → 常駐ポーリング → graceful shutdown を実演するコンソールアプリ
└── tests/KomuraSoft.DesktopHostSample.Tests/     tick 前の停止、tick ごとの scope 生成、例外後の継続などを検証するユニットテスト
```

記事の最小構成例は WPF（`App.xaml.cs` の `OnStartup` / `OnExit`）ですが、このサンプルはどの環境でもビルド・実行できるよう、UI 部分をコンソールアプリで代替しています。`samples/Demo/Program.cs` が `OnStartup`（host の構成と `StartAsync`）、`MainWindow`（`StatusStore` を読む UI 役のループ）、`OnExit`（`StopAsync` の明示的な await）にそれぞれ対応します。`BackgroundService` / `StatusStore` 側のコードは WPF / WinForms でもそのまま使えます。

## 必要環境

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降

## 実行方法

デモ（host を起動し、5 秒周期のポーリングを 2 回観測してから graceful shutdown します。約 11 秒で終了します）:

```console
dotnet run --project samples/Demo
```

テスト（最初の tick 前に止めても graceful に終わること、tick ごとに新しい scope で reader が解決されること、reader の例外でループが死なないこと、Generic Host への登録と起動・停止の往復などを検証します）:

```console
dotnet test
```

## ポイント

- host の起動（`StartAsync`）を UI 表示の前に行い、終了時に `StopAsync` を明示的に await する
- `StartAsync` は起動に参加する短い処理、長く走る本体は `ExecuteAsync`、終了時の整理は `StopAsync` に分ける
- 周期は `PeriodicTimer`、停止は `stoppingToken`、例外はロギングして、`ExecuteAsync` を「管理された while ループ」として書く
- `scoped` な依存（`IDeviceStatusReader`）は hosted service から直接握らず、tick ごとに `IServiceScopeFactory` で scope を切る
- worker は UI を直接触らず、`StatusStore` のような薄い共有層を更新し、UI は自分のコンテキストでそれを読む
- `StopAsync` は正常終了の経路であってクラッシュや強制終了の保険ではないので、後始末を寄せすぎない

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/03/12/002-generic-host-backgroundservice-desktop-app/)をご覧ください。
