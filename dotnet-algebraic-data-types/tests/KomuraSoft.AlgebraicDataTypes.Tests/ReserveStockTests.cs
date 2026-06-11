using KomuraSoft.AlgebraicDataTypes.Stock;
using Xunit;

namespace KomuraSoft.AlgebraicDataTypes.Tests;

// 記事 30 章: bool + out string errorCode を結果型に置き換える
public class ReserveStockTests
{
    private static StockService CreateService(int available)
    {
        var repository = new InMemoryStockRepository();
        repository.Add(new StockItem(new Sku("SKU-001"), available));
        return new StockService(repository);
    }

    [Fact]
    public void 在庫が足りればReservedを返す()
    {
        var service = CreateService(available: 10);

        var result = service.ReserveStock(new Sku("SKU-001"), 3);

        var reserved = Assert.IsType<ReserveStockResult.Reserved>(result);
        Assert.StartsWith("RSV-SKU-001-", reserved.ReservationId.Value);
    }

    [Fact]
    public void 存在しないSKUならSkuNotFoundを返す()
    {
        var service = CreateService(available: 10);

        var result = service.ReserveStock(new Sku("SKU-999"), 1);

        var notFound = Assert.IsType<ReserveStockResult.SkuNotFound>(result);
        Assert.Equal("SKU-999", notFound.Sku.Value);
    }

    [Fact]
    public void 在庫不足ならOutOfStockが要求数と在庫数を持つ()
    {
        var service = CreateService(available: 2);

        var result = service.ReserveStock(new Sku("SKU-001"), 5);

        var outOfStock = Assert.IsType<ReserveStockResult.OutOfStock>(result);
        Assert.Equal("SKU-001", outOfStock.Sku.Value);
        Assert.Equal(5, outOfStock.Requested);
        Assert.Equal(2, outOfStock.Available);
    }

    [Fact]
    public void 呼び出し側は文字列比較をやめてMatchで分岐できる()
    {
        var service = CreateService(available: 2);

        var result = service.ReserveStock(new Sku("SKU-001"), 5);

        string response = result.Match(
            reserved => "200",
            notFound => "404",
            outOfStock => $"409 requested={outOfStock.Requested} available={outOfStock.Available}");

        Assert.Equal("409 requested=5 available=2", response);
    }

    [Fact]
    public void 引当に成功すると在庫が減る()
    {
        var repository = new InMemoryStockRepository();
        repository.Add(new StockItem(new Sku("SKU-001"), 10));
        var service = new StockService(repository);

        service.ReserveStock(new Sku("SKU-001"), 3);

        Assert.Equal(7, repository.Find(new Sku("SKU-001"))!.Available);
    }
}
