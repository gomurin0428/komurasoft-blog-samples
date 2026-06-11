using KomuraSoft.AlgebraicDataTypes.Orders;
using Xunit;

namespace KomuraSoft.AlgebraicDataTypes.Tests;

// 記事 17 章・19 章: ケースそのものがテスト観点になる
public class SubmitOrderResultTests
{
    private readonly OrderSubmissionService service = new OrderSubmissionService(creditLimit: 100_000m);
    private readonly OrderId orderId = new OrderId("ORD-001");

    [Fact]
    public void 正常な注文ならSubmittedを返す()
    {
        var result = service.Submit(orderId, 1000m);

        Assert.IsType<SubmitOrderResult.Submitted>(result);
    }

    [Fact]
    public void 提出済みの注文を再提出するとAlreadySubmittedを返す()
    {
        service.Submit(orderId, 1000m);

        var result = service.Submit(orderId, 1000m);

        Assert.IsType<SubmitOrderResult.AlreadySubmitted>(result);
    }

    [Fact]
    public void 不正な注文ならInvalidOrderを返す()
    {
        var result = service.Submit(orderId, 0m);

        Assert.IsType<SubmitOrderResult.InvalidOrder>(result);
    }

    [Fact]
    public void 与信枠を超えていればCreditLimitExceededを返す()
    {
        var result = service.Submit(orderId, 100_001m);

        var exceeded = Assert.IsType<SubmitOrderResult.CreditLimitExceeded>(result);
        Assert.Equal(100_000m, exceeded.Limit);
    }

    [Fact]
    public void テストデータは1行で意図を持って作れる()
    {
        // 記事 19 章: 「与信超過」という意味を持つデータが 1 行で作れる
        var limit = 100_000m;
        var result = SubmitOrderResult.OverCreditLimit(limit);

        var exceeded = Assert.IsType<SubmitOrderResult.CreditLimitExceeded>(result);
        Assert.Equal(limit, exceeded.Limit);
    }

    [Fact]
    public void Matchは全ケースを処理できる()
    {
        SubmitOrderResult[] results =
        [
            SubmitOrderResult.Success(orderId),
            SubmitOrderResult.DuplicateSubmission(orderId),
            SubmitOrderResult.Invalid("明細がありません。"),
            SubmitOrderResult.OverCreditLimit(100_000m),
        ];

        string[] messages = results
            .Select(result => result.Match(
                submitted => $"submitted: {submitted.OrderId.Value}",
                alreadySubmitted => $"already: {alreadySubmitted.OrderId.Value}",
                invalidOrder => $"invalid: {invalidOrder.Reason}",
                creditLimitExceeded => $"limit: {creditLimitExceeded.Limit}"))
            .ToArray();

        string[] expected =
            ["submitted: ORD-001", "already: ORD-001", "invalid: 明細がありません。", "limit: 100000"];
        Assert.Equal(expected, messages);
    }
}
