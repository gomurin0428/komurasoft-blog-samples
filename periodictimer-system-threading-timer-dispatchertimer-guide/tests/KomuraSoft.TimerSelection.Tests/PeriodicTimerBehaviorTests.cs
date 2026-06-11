using Xunit;

namespace KomuraSoft.TimerSelection.Tests;

/// <summary>
/// 記事 3.1 / 5.3 で触れている PeriodicTimer の性質を、コードとして確認するテストです。
/// </summary>
public class PeriodicTimerBehaviorTests
{
    [Fact]
    public async Task WaitForNextTickAsync_ReturnsFalse_AfterDispose()
    {
        var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        ValueTask<bool> waiting = timer.WaitForNextTickAsync();
        timer.Dispose();

        // Dispose されたタイマーの待機は false で抜ける（停止の合図になる）
        Assert.False(await waiting);
        Assert.False(await timer.WaitForNextTickAsync());
    }

    [Fact]
    public async Task WaitForNextTickAsync_Throws_WhenCanceled()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // CancellationToken のキャンセルは OperationCanceledException として観測される
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await timer.WaitForNextTickAsync(cts.Token));
    }

    [Fact]
    public async Task MissedTicks_AreCoalescedIntoOne()
    {
        // 周期 250ms のタイマーを、誰も待っていない状態で 1.5 秒放置する。
        // この間に複数回 tick しているはずだが、溜まった tick は 1 回に畳まれる。
        // （「勝手に追いついてくれる」と誤解しないための確認。記事 5.3）
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
        await Task.Delay(TimeSpan.FromMilliseconds(1500));

        // 1 回目の待機は、溜まっていた tick によって即完了する
        ValueTask<bool> first = timer.WaitForNextTickAsync();
        Assert.True(first.IsCompleted);
        Assert.True(await first);

        // 2 回目の待機は即完了しない（複数回ぶんの tick は残っていない）
        ValueTask<bool> second = timer.WaitForNextTickAsync();
        Assert.False(second.IsCompleted);

        timer.Dispose();
        Assert.False(await second);
    }
}
