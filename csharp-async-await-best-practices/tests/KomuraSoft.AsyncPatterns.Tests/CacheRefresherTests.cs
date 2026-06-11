using Xunit;

namespace KomuraSoft.AsyncPatterns.Tests;

public class CacheRefresherTests
{
    [Fact]
    public async Task RefreshAsync_SerializesConcurrentCalls()
    {
        int concurrency = 0;
        int maxConcurrency = 0;
        int callCount = 0;

        var refresher = new CacheRefresher(async _ =>
        {
            int current = Interlocked.Increment(ref concurrency);
            InterlockedMax(ref maxConcurrency, current);
            Interlocked.Increment(ref callCount);

            await Task.Delay(30);

            Interlocked.Decrement(ref concurrency);
        });

        Task[] refreshes = Enumerable.Range(0, 5)
            .Select(_ => refresher.RefreshAsync(CancellationToken.None))
            .ToArray();
        await Task.WhenAll(refreshes).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(5, callCount);
        Assert.Equal(1, maxConcurrency); // SemaphoreSlim(1, 1) なので常に 1 件ずつ
    }

    [Fact]
    public async Task RefreshAsync_ReleasesGate_EvenWhenCoreThrows()
    {
        int callCount = 0;
        var refresher = new CacheRefresher(_ =>
        {
            Interlocked.Increment(ref callCount);
            throw new InvalidOperationException("refresh failed");
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => refresher.RefreshAsync(CancellationToken.None));

        // Release が finally に入っているので、失敗後も次の呼び出しが詰まらない
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => refresher.RefreshAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.Equal(2, callCount);
    }

    private static void InterlockedMax(ref int location, int value)
    {
        int current = Volatile.Read(ref location);
        while (value > current)
        {
            int original = Interlocked.CompareExchange(ref location, value, current);
            if (original == current)
            {
                return;
            }

            current = original;
        }
    }
}
