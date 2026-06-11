namespace KomuraSoft.AlgebraicDataTypes.Stock;

// 記事 30 章: 既存コードへのリファクタリング例
// bool TryReserveStock(..., out string errorCode) を結果型に置き換えたサービス

public interface IStockRepository
{
    StockItem? Find(Sku sku);
}

public sealed class StockItem
{
    private int _nextReservationNumber = 1;

    public StockItem(Sku sku, int available)
    {
        if (sku == null) throw new ArgumentNullException(nameof(sku));
        if (available < 0) throw new ArgumentOutOfRangeException(nameof(available));
        Sku = sku;
        Available = available;
    }

    public Sku Sku { get; }
    public int Available { get; private set; }

    public ReservationId Reserve(int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (quantity > Available)
        {
            throw new InvalidOperationException("在庫が足りません。");
        }

        Available -= quantity;
        return new ReservationId($"RSV-{Sku.Value}-{_nextReservationNumber++:D4}");
    }
}

public sealed class InMemoryStockRepository : IStockRepository
{
    private readonly Dictionary<string, StockItem> _items = new Dictionary<string, StockItem>();

    public void Add(StockItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        _items[item.Sku.Value] = item;
    }

    public StockItem? Find(Sku sku)
    {
        if (sku == null) throw new ArgumentNullException(nameof(sku));
        return _items.TryGetValue(sku.Value, out var item) ? item : null;
    }
}

public sealed class StockService
{
    private readonly IStockRepository stockRepository;

    public StockService(IStockRepository stockRepository)
    {
        if (stockRepository == null) throw new ArgumentNullException(nameof(stockRepository));
        this.stockRepository = stockRepository;
    }

    public ReserveStockResult ReserveStock(Sku sku, int quantity)
    {
        var stock = stockRepository.Find(sku);
        if (stock == null)
        {
            return ReserveStockResult.NotFound(sku);
        }

        if (stock.Available < quantity)
        {
            return ReserveStockResult.NotEnough(sku, quantity, stock.Available);
        }

        var reservationId = stock.Reserve(quantity);
        return ReserveStockResult.Success(reservationId);
    }
}
