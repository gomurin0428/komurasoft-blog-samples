# .NET の汎用ホスト(Generic Host)サンプル

ブログ記事「.NET の汎用ホスト(Generic Host)とは — DI・構成・ロギング・BackgroundService をまとめて理解する」のサンプルコードです。

- 記事(日本語): https://comcomponent.com/blog/2026/03/14/000-dotnet-generic-host-what-is/
- 記事(English): https://comcomponent.com/en/blog/2026/03/14/000-dotnet-generic-host-what-is/

## このサンプルで示していること

汎用ホストが「アプリの組み立てとライフタイム管理を 1 か所に集める箱」であることを、最小構成のコンソールアプリで示します。

| 要素 | ファイル | 内容 |
| --- | --- | --- |
| ホストの組み立て | `Program.cs` | `Host.CreateApplicationBuilder` で構成・ロギング・DI・ライフタイムを一括初期化 |
| 構成(オプションパターン) | `GreetingOptions.cs` / `appsettings.json` | `Greeting` セクションを型付きクラスにバインド |
| DI | `GreetingService.cs` | インターフェイス + コンストラクタインジェクション |
| 常駐処理 | `Worker.cs` | `BackgroundService` + `PeriodicTimer` による定期実行 |
| グレースフルシャットダウン | `Worker.cs` | Ctrl+C / SIGTERM を `CancellationToken` で受けて後始末 |

## 実行方法

.NET 8 SDK 以降が必要です。

```bash
cd dotnet-generic-host-what-is
dotnet run
```

3 秒ごとに挨拶メッセージがログ出力されます。Ctrl+C で停止すると、ワーカーが後始末を済ませてから終了する様子が確認できます。

構成は `appsettings.json` のほか、環境変数やコマンドライン引数でも上書きできます。

```bash
# コマンドライン引数で上書き
dotnet run -- --Greeting:Message="こんにちは" --Greeting:IntervalSeconds=1

# 環境変数で上書き (bash)
Greeting__Message="Hello from env" dotnet run
```
