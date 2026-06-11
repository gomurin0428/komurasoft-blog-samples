using KomuraSoft.AlgebraicDataTypes.Classic;
using Xunit;

namespace KomuraSoft.AlgebraicDataTypes.Tests;

// 記事 5 章: private コンストラクターで閉じた集合にする
public class PaymentResultClassicTests
{
    [Fact]
    public void SuccessはReceiptNoを持つSucceededを返す()
    {
        PaymentResult result = PaymentResult.Success("R-001");

        var succeeded = Assert.IsType<PaymentResult.Succeeded>(result);
        Assert.Equal("R-001", succeeded.ReceiptNo);
    }

    [Fact]
    public void InsufficientはShortageを持つInsufficientFundsを返す()
    {
        PaymentResult result = PaymentResult.Insufficient(1500m);

        var insufficient = Assert.IsType<PaymentResult.InsufficientFunds>(result);
        Assert.Equal(1500m, insufficient.Shortage);
    }
}
