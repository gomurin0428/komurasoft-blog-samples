using DesktopHostSample;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// 記事 5.1 の App.xaml.cs (WPF) と同じ流れをコンソールで再現するデモです。
// - OnStartup 相当: builder で DI / HostOptions / hosted service を構成し、StartAsync で起動する
// - MainWindow 相当: UI 役のループが StatusStore.Current を 1 秒ごとに読んで表示する
// - OnExit 相当: StopAsync を明示的に await して graceful shutdown する
// WPF / WinForms では MainWindow / MainForm の表示と終了イベントがここに対応します。

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddSingleton<StatusStore>();
builder.Services.AddScoped<IDeviceStatusReader, DeviceStatusReader>();
builder.Services.AddHostedService<DevicePollingBackgroundService>();

using IHost host = builder.Build();

// host の起動を UI 表示の前に行う（記事 5.1 のポイント 1）
await host.StartAsync();

StatusStore statusStore = host.Services.GetRequiredService<StatusStore>();

Console.WriteLine("[ui] main window shown (console stands in for WPF MainWindow)");

// BackgroundService は 5 秒周期でポーリングするので、
// 11 秒間だけ UI 役として状態を読み、2 回分の更新を観測してから終了する。
for (int i = 1; i <= 11; i++)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    Console.WriteLine($"[ui] t={i,2}s StatusStore.Current = {statusStore.Current.Message}");
}

// 終了時に StopAsync を明示的に await する（記事 5.1 のポイント 2）
Console.WriteLine("[ui] exiting: stopping host...");
await host.StopAsync();
Console.WriteLine("[demo] graceful shutdown completed");
