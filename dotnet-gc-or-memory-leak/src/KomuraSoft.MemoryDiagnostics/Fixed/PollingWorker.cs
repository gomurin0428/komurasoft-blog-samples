namespace KomuraSoft.MemoryDiagnostics.Fixed;

/// <summary>
/// Timer の破棄漏れの修正例（記事 11.4 章）。
/// 一時的なオブジェクトとして使うなら、Dispose で Timer を破棄する設計にします。
/// </summary>
public sealed class PollingWorker : IDisposable
{
    private readonly Timer _timer;

    public PollingWorker()
    {
        _timer = new Timer(_ => Poll(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    private void Poll()
    {
        // polling
    }
}
