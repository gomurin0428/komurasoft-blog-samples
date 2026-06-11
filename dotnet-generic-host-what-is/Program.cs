using GenericHostSample;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// 汎用ホスト(Generic Host)の最小構成サンプル。
// Host.CreateApplicationBuilder が以下をまとめて初期化する:
//   - 構成 (appsettings.json / 環境変数 / コマンドライン引数)
//   - ロギング (コンソール出力)
//   - DI コンテナ
//   - ライフタイム管理 (Ctrl+C / SIGTERM のハンドリング)
var builder = Host.CreateApplicationBuilder(args);

// 構成の "Greeting" セクションを型付きオプションとして登録
builder.Services.Configure<GreetingOptions>(
    builder.Configuration.GetSection(GreetingOptions.SectionName));

// アプリケーションサービスを DI に登録
builder.Services.AddSingleton<IGreetingService, GreetingService>();

// 常駐処理 (BackgroundService) を登録
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// RunAsync はホストが停止するまで戻らない。
// Ctrl+C / SIGTERM を受けると登録済みサービスを順に停止してから終了する。
await host.RunAsync();
