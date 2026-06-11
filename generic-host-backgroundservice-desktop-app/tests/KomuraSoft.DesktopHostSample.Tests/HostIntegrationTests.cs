using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DesktopHostSample.Tests;

public class HostIntegrationTests
{
    [Fact]
    public async Task Host_StartAsyncAndStopAsync_RunHostedServiceGracefully()
    {
        // 記事 5.1 の App.xaml.cs と同じ登録を Generic Host に対して行い、
        // StartAsync / StopAsync の往復が graceful に完了することを確認する。
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.Services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(15);
        });

        builder.Services.AddSingleton<ReadProbe>();
        builder.Services.AddSingleton<StatusStore>();
        builder.Services.AddScoped<IDeviceStatusReader, FakeDeviceStatusReader>();
        builder.Services.AddHostedService<DevicePollingBackgroundService>();

        using IHost host = builder.Build();

        await host.StartAsync();

        var store = host.Services.GetRequiredService<StatusStore>();
        Assert.Equal(DeviceStatus.Empty, store.Current);

        // OnExit 相当: StopAsync を明示的に await する
        await host.StopAsync();
    }
}
