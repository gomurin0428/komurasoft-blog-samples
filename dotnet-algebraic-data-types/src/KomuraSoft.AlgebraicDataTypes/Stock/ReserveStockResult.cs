namespace KomuraSoft.AlgebraicDataTypes.Stock;

// 記事 18 章・30 章: 在庫引当の結果型
// bool + out string errorCode を、意味を持った結果型に置き換える
public abstract class ReserveStockResult
{
    private ReserveStockResult()
    {
    }

    public sealed class Reserved : ReserveStockResult
    {
        internal Reserved(ReservationId reservationId)
        {
            ReservationId = reservationId;
        }

        public ReservationId ReservationId { get; }
    }

    public sealed class SkuNotFound : ReserveStockResult
    {
        internal SkuNotFound(Sku sku)
        {
            Sku = sku;
        }

        public Sku Sku { get; }
    }

    public sealed class OutOfStock : ReserveStockResult
    {
        internal OutOfStock(Sku sku, int requested, int available)
        {
            Sku = sku;
            Requested = requested;
            Available = available;
        }

        public Sku Sku { get; }
        public int Requested { get; }
        public int Available { get; }
    }

    public static ReserveStockResult Success(ReservationId reservationId)
        => new Reserved(reservationId);

    public static ReserveStockResult NotFound(Sku sku)
        => new SkuNotFound(sku);

    public static ReserveStockResult NotEnough(Sku sku, int requested, int available)
        => new OutOfStock(sku, requested, available);

    public T Match<T>(
        Func<Reserved, T> reserved,
        Func<SkuNotFound, T> skuNotFound,
        Func<OutOfStock, T> outOfStock)
    {
        if (reserved == null) throw new ArgumentNullException(nameof(reserved));
        if (skuNotFound == null) throw new ArgumentNullException(nameof(skuNotFound));
        if (outOfStock == null) throw new ArgumentNullException(nameof(outOfStock));

        var r = this as Reserved;
        if (r != null) return reserved(r);

        var n = this as SkuNotFound;
        if (n != null) return skuNotFound(n);

        var o = this as OutOfStock;
        if (o != null) return outOfStock(o);

        throw new InvalidOperationException("未知の在庫引当結果です。");
    }
}
