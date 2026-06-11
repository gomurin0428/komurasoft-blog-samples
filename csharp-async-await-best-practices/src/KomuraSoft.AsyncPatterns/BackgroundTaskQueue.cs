using System.Threading.Channels;

namespace KomuraSoft.AsyncPatterns;

/// <summary>
/// 「今すぐ終わらなくてよいが、確実に処理したい」仕事をキューに積み、
/// 専用のコンシューマが順番に処理するための Channel ベースのキューです（記事 3.7）。
/// BoundedChannelFullMode.Wait により、キューが満杯なら書き込み側を待たせます（backpressure）。
/// 素の fire-and-forget（Task.Run の投げっぱなし）より、例外・停止・並列数・上限を扱いやすくなります。
/// </summary>
public sealed class BackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue =
        Channel.CreateBounded<Func<CancellationToken, ValueTask>>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

    public ValueTask EnqueueAsync(
        Func<CancellationToken, ValueTask> workItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        return _queue.Writer.WriteAsync(workItem, cancellationToken);
    }

    public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
        => _queue.Reader.ReadAsync(cancellationToken);
}
