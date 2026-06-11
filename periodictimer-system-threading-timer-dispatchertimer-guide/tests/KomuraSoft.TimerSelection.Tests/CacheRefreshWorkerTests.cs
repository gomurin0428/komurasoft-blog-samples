using Xunit;
using static KomuraSoft.TimerSelection.Tests.TestHelpers;

namespace KomuraSoft.TimerSelection.Tests;

public class CacheRefreshWorkerTests
{
    [Fact]
    public async Task RefreshesOnceOnStart_AndStopsGracefully()
    {
        var logger = new ListLogger<CacheRefreshWorker>();
        using var worker = new CacheRefreshWorker(logger);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            // 起動直後（タイマーの最初の tick を待たずに）1 回目の更新が走る
            Assert.True(
                await WaitUntilAsync(() => logger.Contains("CacheRefreshWorker started."), TimeSpan.FromSeconds(10)),
                "started log was not observed.");
            Assert.True(
                await WaitUntilAsync(() => logger.Contains("Refreshing cache..."), TimeSpan.FromSeconds(10)),
                "initial refresh log was not observed.");

            // 初回の RefreshCacheAsync（約 1 秒）が終わって WaitForNextTickAsync に
            // 入るまで、余裕を持って待つ（周期は 5 分なので 2 回目の更新は走らない）
            await Task.Delay(TimeSpan.FromSeconds(3));
            Assert.Equal(1, logger.Count("Refreshing cache..."));
        }
        finally
        {
            // StopAsync で stoppingToken がキャンセルされ、WaitForNextTickAsync が
            // OperationCanceledException を投げて、ループがきれいに終わる
            await worker.StopAsync(CancellationToken.None);
        }

        Assert.True(
            await WaitUntilAsync(() => logger.Contains("CacheRefreshWorker stopping."), TimeSpan.FromSeconds(10)),
            "stopping log was not observed after StopAsync.");
    }
}
