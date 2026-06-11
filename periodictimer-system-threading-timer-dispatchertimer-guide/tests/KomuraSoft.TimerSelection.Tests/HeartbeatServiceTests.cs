using Xunit;
using static KomuraSoft.TimerSelection.Tests.TestHelpers;

namespace KomuraSoft.TimerSelection.Tests;

public class HeartbeatServiceTests
{
    [Fact]
    public async Task WritesHeartbeat_AfterStart()
    {
        var logger = new ListLogger<HeartbeatService>();
        using var service = new HeartbeatService(logger);

        await service.StartAsync(CancellationToken.None);
        try
        {
            // dueTime = TimeSpan.Zero なので最初の heartbeat はすぐ ThreadPool で走る
            // （タイミング依存にならないよう、余裕を持って最大 10 秒待つ）
            Assert.True(
                await WaitUntilAsync(() => logger.Count("Heartbeat") >= 1, TimeSpan.FromSeconds(10)),
                "no heartbeat was observed after StartAsync.");
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartStopDispose_DoesNotThrow()
    {
        var logger = new ListLogger<HeartbeatService>();
        var service = new HeartbeatService(logger);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // Dispose 後でも、すでにキューされた callback が走ることはありうる、
        // という記事の注意点どおり、ここでは例外が出ないことだけを確認する
        service.Dispose();
    }
}
