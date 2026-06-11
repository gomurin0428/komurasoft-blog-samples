using KomuraSoft.TimerSelection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// PeriodicTimer（CacheRefreshWorker）と System.Threading.Timer（HeartbeatService）を
// 同じ汎用ホスト上で動かして、出力の違いを観察するデモです。
//
// - CacheRefreshWorker: 起動直後に 1 回キャッシュ更新し、以後は PeriodicTimer の
//   tick を await して 5 分おきに回る async ループ（デモ中は初回の 1 回だけ見える）
// - HeartbeatService:   ThreadPool callback が dueTime = 0 / 周期 5 秒で起動される
//   （デモ中は 0 秒・5 秒時点の 2 回の heartbeat が見える）
//
// DispatcherTimer は WPF（UI スレッド）専用のため、このデモには含めていません。
// docs/MainWindowClock.cs.txt と README を参照してください。

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss.fff ";
        });
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<CacheRefreshWorker>();
        services.AddHostedService<HeartbeatService>();
    })
    .Build();

Console.WriteLine("[demo] starting host (runs for about 7 seconds)");

await host.StartAsync();
await Task.Delay(TimeSpan.FromSeconds(7));

Console.WriteLine("[demo] stopping host");
await host.StopAsync();

Console.WriteLine("[demo] done");
