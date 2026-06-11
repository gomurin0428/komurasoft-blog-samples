namespace KomuraSoft.MemoryDiagnostics.Fixed;

/// <summary>
/// イベント購読解除漏れの修正例（記事 11.3 章）。
/// Dispose で購読を解除すれば、publisher から subscriber への参照が外れ、
/// ViewModel は GC で回収できるようになります。
/// </summary>
public sealed class OrderViewModel : IDisposable
{
    private readonly OrderService _service;

    public OrderViewModel(OrderService service)
    {
        _service = service;
        _service.OrderChanged += OnOrderChanged;
    }

    public void Dispose()
    {
        _service.OrderChanged -= OnOrderChanged;
    }

    private void OnOrderChanged(object? sender, OrderChangedEventArgs e)
    {
        // update view model
    }
}
