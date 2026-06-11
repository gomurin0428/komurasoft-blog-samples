namespace KomuraSoft.MemoryDiagnostics.Leaky;

/// <summary>
/// Timer の破棄漏れ（記事 11.4 章）。
/// アクティブな System.Threading.Timer はランタイムのタイマーキューに登録され、
/// コールバックの delegate 経由でこの Worker への参照がつながるため、
/// 参照を手放しても GC で回収されません。
/// </summary>
public sealed class PollingWorker
{
    private readonly Timer _timer;

    public PollingWorker()
    {
        _timer = new Timer(_ => Poll(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    private void Poll()
    {
        // polling
    }
}
