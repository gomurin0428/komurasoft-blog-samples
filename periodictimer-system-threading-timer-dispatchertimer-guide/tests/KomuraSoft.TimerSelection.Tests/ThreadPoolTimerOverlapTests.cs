using Xunit;
using static KomuraSoft.TimerSelection.Tests.TestHelpers;

namespace KomuraSoft.TimerSelection.Tests;

/// <summary>
/// 記事 4.2 で触れている「System.Threading.Timer の callback は前回の完了を待たずに
/// 重なりうる」という性質と、Interlocked.Exchange によるガードの効果を確認するテストです。
/// </summary>
public class ThreadPoolTimerOverlapTests
{
    [Fact]
    public async Task Callbacks_CanOverlap_WhenWorkOutlastsPeriod()
    {
        // ThreadPool のスレッド注入待ちで結果がぶれないよう、最小スレッド数を確保する
        ThreadPool.GetMinThreads(out int workerThreads, out int ioThreads);
        ThreadPool.SetMinThreads(Math.Max(workerThreads, 8), ioThreads);

        int current = 0;
        int maxObserved = 0;

        // 周期 50ms に対して処理 300ms。ガードしなければ callback は重なる
        using var timer = new Timer(_ =>
        {
            int now = Interlocked.Increment(ref current);
            InterlockedMax(ref maxObserved, now);
            Thread.Sleep(300);
            Interlocked.Decrement(ref current);
        }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(50));

        bool overlapped = await WaitUntilAsync(
            () => Volatile.Read(ref maxObserved) >= 2,
            TimeSpan.FromSeconds(10));

        timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        Assert.True(overlapped, "timer callbacks did not overlap within the timeout.");
    }

    [Fact]
    public async Task InterlockedExchangeGuard_PreventsOverlap()
    {
        int running = 0; // 記事の HeartbeatService と同じガード
        int current = 0;
        int maxConcurrent = 0;
        int executed = 0;
        int skipped = 0;

        using var timer = new Timer(_ =>
        {
            if (Interlocked.Exchange(ref running, 1) != 0)
            {
                Interlocked.Increment(ref skipped);
                return;
            }

            try
            {
                int now = Interlocked.Increment(ref current);
                InterlockedMax(ref maxConcurrent, now);
                Thread.Sleep(200);
                Interlocked.Decrement(ref current);
                Interlocked.Increment(ref executed);
            }
            finally
            {
                Volatile.Write(ref running, 0);
            }
        }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(50));

        // 実行が複数回観測でき、かつ実行中に発火した callback がスキップされるまで待つ
        bool observed = await WaitUntilAsync(
            () => Volatile.Read(ref executed) >= 2 && Volatile.Read(ref skipped) >= 1,
            TimeSpan.FromSeconds(10));

        timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        // Change の直後でも、すでにキューされた callback が走りうるので少し待ってから検証する
        await Task.Delay(500);

        Assert.True(observed, "guarded callback executions / skips were not observed within the timeout.");
        Assert.Equal(1, Volatile.Read(ref maxConcurrent)); // ガードがあれば同時実行は常に 1
    }
}
