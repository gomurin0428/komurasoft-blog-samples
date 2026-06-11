using Xunit;

namespace KomuraSoft.AsyncPatterns.Tests;

public class BackgroundTaskQueueTests
{
    [Fact]
    public async Task EnqueueThenDequeue_ProcessesWorkItemsInOrder()
    {
        var queue = new BackgroundTaskQueue();
        var processed = new List<int>();

        for (int i = 1; i <= 3; i++)
        {
            int number = i;
            await queue.EnqueueAsync(_ =>
            {
                processed.Add(number);
                return ValueTask.CompletedTask;
            });
        }

        for (int i = 0; i < 3; i++)
        {
            Func<CancellationToken, ValueTask> workItem =
                await queue.DequeueAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            await workItem(CancellationToken.None);
        }

        Assert.Equal([1, 2, 3], processed);
    }

    [Fact]
    public async Task EnqueueAsync_WaitsWhenQueueIsFull_Backpressure()
    {
        var queue = new BackgroundTaskQueue();
        static ValueTask NoOp(CancellationToken _) => ValueTask.CompletedTask;

        // 上限（100 件）までは待たずに入る
        for (int i = 0; i < 100; i++)
        {
            await queue.EnqueueAsync(NoOp).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        }

        // 101 件目は、キューが満杯なので空きが出るまで待たされる（BoundedChannelFullMode.Wait）
        Task overflowWrite = queue.EnqueueAsync(NoOp).AsTask();
        await Task.Delay(100);
        Assert.False(overflowWrite.IsCompleted);

        // コンシューマが 1 件取り出すと、待っていた書き込みが完了する
        await queue.DequeueAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        await overflowWrite.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DequeueAsync_CanBeCancelledWhileWaiting()
    {
        var queue = new BackgroundTaskQueue();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => queue.DequeueAsync(cts.Token).AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task EnqueueAsync_ThrowsOnNullWorkItem()
    {
        var queue = new BackgroundTaskQueue();

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await queue.EnqueueAsync(null!));
    }
}
