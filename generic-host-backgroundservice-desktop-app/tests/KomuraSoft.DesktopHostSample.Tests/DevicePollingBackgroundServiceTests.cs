using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DesktopHostSample.Tests;

public class DevicePollingBackgroundServiceTests
{
    private static ServiceProvider BuildProvider<TReader>()
        where TReader : class, IDeviceStatusReader
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ReadProbe>();
        services.AddSingleton<StatusStore>();
        services.AddScoped<IDeviceStatusReader, TReader>();
        services.AddSingleton<DevicePollingBackgroundService>();
        return services.BuildServiceProvider();
    }

    private static async Task<DeviceStatus> WaitForUpdateAsync(
        StatusStore store,
        DeviceStatus previous,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (Equals(store.Current, previous))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
        }

        return store.Current;
    }

    [Fact]
    public async Task StopAsync_BeforeFirstTick_CompletesGracefully()
    {
        // 周期は 5 秒なので、最初の tick が来る前に止めれば store は Empty のまま。
        await using ServiceProvider provider = BuildProvider<FakeDeviceStatusReader>();
        var store = provider.GetRequiredService<StatusStore>();
        var service = provider.GetRequiredService<DevicePollingBackgroundService>();

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

        Assert.NotNull(service.ExecuteTask);
        Assert.True(service.ExecuteTask!.IsCompleted);
        Assert.Equal(DeviceStatus.Empty, store.Current);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesStatusStore_OnEachTick_WithFreshScope()
    {
        await using ServiceProvider provider = BuildProvider<FakeDeviceStatusReader>();
        var store = provider.GetRequiredService<StatusStore>();
        var probe = provider.GetRequiredService<ReadProbe>();
        var service = provider.GetRequiredService<DevicePollingBackgroundService>();

        await service.StartAsync(CancellationToken.None);
        try
        {
            DeviceStatus first = await WaitForUpdateAsync(
                store, DeviceStatus.Empty, TimeSpan.FromSeconds(15));
            DeviceStatus second = await WaitForUpdateAsync(
                store, first, TimeSpan.FromSeconds(15));

            Assert.Equal("read-1 (instance 1)", first.Message);
            Assert.Equal("read-2 (instance 2)", second.Message);

            // scoped な reader は tick ごとに新しい scope で解決される（記事 5.2 / 6.2）
            Assert.True(probe.InstanceCount >= 2);
        }
        finally
        {
            await service.StopAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
        }
    }

    [Fact]
    public async Task ExecuteAsync_SurvivesReaderException_AndKeepsPolling()
    {
        // 1 回目の読み取りは例外、2 回目以降は成功する reader。
        // ループが「例外でも無言で死なない」こと（記事 6.2 のコツ 2）を確認する。
        await using ServiceProvider provider = BuildProvider<FlakyDeviceStatusReader>();
        var store = provider.GetRequiredService<StatusStore>();
        var service = provider.GetRequiredService<DevicePollingBackgroundService>();

        await service.StartAsync(CancellationToken.None);
        try
        {
            DeviceStatus status = await WaitForUpdateAsync(
                store, DeviceStatus.Empty, TimeSpan.FromSeconds(20));

            Assert.Equal("recovered (read-2)", status.Message);
        }
        finally
        {
            await service.StopAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
        }
    }
}
