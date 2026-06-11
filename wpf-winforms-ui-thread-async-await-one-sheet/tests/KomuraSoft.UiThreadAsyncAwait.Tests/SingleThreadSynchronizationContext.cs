using System.Collections.Concurrent;

namespace KomuraSoft.UiThreadAsyncAwait.Tests;

/// <summary>
/// WPF / WinForms の UI スレッドを模した、専用スレッド 1 本 + キュー（メッセージループ相当）の
/// SynchronizationContext。
/// plain await が「キャプチャしたコンテキストへ戻る」こと、
/// ConfigureAwait(false) が「戻りを強制しない」こと、
/// .Result がそのスレッドを塞いでデッドロックすることを、UI 非依存で再現するために使う。
/// </summary>
internal sealed class SingleThreadSynchronizationContext : SynchronizationContext
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
    private readonly Thread _thread;

    public SingleThreadSynchronizationContext()
    {
        _thread = new Thread(RunMessageLoop)
        {
            IsBackground = true,
            Name = "FakeUiThread",
        };
        _thread.Start();
    }

    /// <summary>UI スレッド相当のスレッド ID。</summary>
    public int ThreadId => _thread.ManagedThreadId;

    public override void Post(SendOrPostCallback d, object? state)
    {
        try
        {
            _queue.Add((d, state));
        }
        catch (InvalidOperationException)
        {
            // ループ終了後の投稿は捨てる（テスト用の簡易実装）
        }
    }

    public override void Send(SendOrPostCallback d, object? state)
        => throw new NotSupportedException("このテスト用コンテキストは Post のみ対応です。");

    /// <summary>メッセージループを終了させる。</summary>
    public void Complete() => _queue.CompleteAdding();

    private void RunMessageLoop()
    {
        SetSynchronizationContext(this);

        foreach ((SendOrPostCallback callback, object? state) in _queue.GetConsumingEnumerable())
        {
            callback(state);
        }
    }
}
