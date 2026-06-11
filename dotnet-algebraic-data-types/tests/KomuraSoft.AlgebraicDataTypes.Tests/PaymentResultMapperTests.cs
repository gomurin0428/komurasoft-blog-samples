using KomuraSoft.AlgebraicDataTypes.Records;
using Xunit;

namespace KomuraSoft.AlgebraicDataTypes.Tests;

// 記事 14 章: API境界ではDTOに変換する
public class PaymentResultMapperTests
{
    [Fact]
    public void SucceededはtypeとreceiptNoを持つDTOになる()
    {
        var dto = PaymentResultMapper.ToDto(new PaymentResult.Succeeded("R-001"));

        Assert.Equal("succeeded", dto.Type);
        Assert.Equal("R-001", dto.ReceiptNo);
        Assert.Null(dto.Reason);
        Assert.Null(dto.Message);
    }

    [Fact]
    public void Rejectedはtypeとreasonを持つDTOになる()
    {
        var dto = PaymentResultMapper.ToDto(new PaymentResult.Rejected("card_expired"));

        Assert.Equal("rejected", dto.Type);
        Assert.Equal("card_expired", dto.Reason);
        Assert.Null(dto.ReceiptNo);
    }

    [Fact]
    public void NetworkFailureはtypeとmessageを持つDTOになる()
    {
        var dto = PaymentResultMapper.ToDto(new PaymentResult.NetworkFailure("timeout"));

        Assert.Equal("network_failure", dto.Type);
        Assert.Equal("timeout", dto.Message);
        Assert.Null(dto.ReceiptNo);
    }
}
