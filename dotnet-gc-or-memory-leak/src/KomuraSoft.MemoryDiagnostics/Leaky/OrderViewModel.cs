namespace KomuraSoft.MemoryDiagnostics.Leaky;

/// <summary>
/// イベント購読解除漏れ（記事 11.3 章）。
/// OrderService が singleton で、この ViewModel が画面ごとに作られる場合、
/// OrderService のイベント delegate が ViewModel を参照し続けるため、
/// 画面を閉じても ViewModel は GC されません。
/// </summary>
public sealed class OrderViewModel
{
    private readonly OrderService _service;

    public OrderViewModel(OrderService service)
    {
        _service = service;
        _service.OrderChanged += OnOrderChanged;
    }

    private void OnOrderChanged(object? sender, OrderChangedEventArgs e)
    {
        // update view model
    }
}
