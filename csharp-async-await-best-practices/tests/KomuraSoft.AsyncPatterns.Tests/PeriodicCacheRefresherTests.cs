using Xunit;

namespace KomuraSoft.AsyncPatterns.Tests;

public class PeriodicCacheRefresherTests
{
    [Fact]
    public async Task RunPeriodicAsync_RefreshesRepeatedly_UntilCancelled()
    {
        int refreshCount = 0;
        using var cts = new CancellationTokenSource();

        var refresher = new PeriodicCacheRefresher(TimeSpan.FromMilliseconds(10), _ =>
        {
            if (Interlocked.Increment(ref refreshCount) >= 3)
            {
                cts.Cancel();
            }

            return Task.CompletedTask;
        });

        // 停止は CancellationToken で行う（WaitForNextTickAsync が OperationCanceledException を投げる）
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => refresher.RunPeriodicAsync(cts.Token).WaitAsync(TimeSpan.FromSeconds(10)));

        Assert.True(refreshCount >= 3, $"Expected at least 3 refreshes, but got {refreshCount}.");
    }

    [Fact]
    public async Task RunPeriodicAsync_DoesNotRefresh_WhenCancelledBeforeFirstTick()
    {
        int refreshCount = 0;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var refresher = new PeriodicCacheRefresher(TimeSpan.FromSeconds(10), _ =>
        {
            Interlocked.Increment(ref refreshCount);
            return Task.CompletedTask;
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => refresher.RunPeriodicAsync(cts.Token).WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.Equal(0, refreshCount);
    }
}
