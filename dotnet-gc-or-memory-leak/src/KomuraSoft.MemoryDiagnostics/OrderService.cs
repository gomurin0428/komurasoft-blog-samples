namespace KomuraSoft.MemoryDiagnostics;

public sealed class OrderChangedEventArgs : EventArgs
{
    public OrderChangedEventArgs(string orderId)
    {
        OrderId = orderId;
    }

    public string OrderId { get; }
}

/// <summary>
/// 記事 11.3 章の publisher 側。
/// singleton のような長命オブジェクトを想定しています。
/// イベントの delegate が subscriber（ViewModel）への参照を持ち続けます。
/// </summary>
public sealed class OrderService
{
    public event EventHandler<OrderChangedEventArgs>? OrderChanged;

    public void RaiseOrderChanged(string orderId)
    {
        OrderChanged?.Invoke(this, new OrderChangedEventArgs(orderId));
    }

    /// <summary>購読中のハンドラー数（観測用）。</summary>
    public int HandlerCount => OrderChanged?.GetInvocationList().Length ?? 0;
}
