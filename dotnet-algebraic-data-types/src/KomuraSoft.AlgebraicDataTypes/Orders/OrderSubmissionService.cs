namespace KomuraSoft.AlgebraicDataTypes.Orders;

// 記事 17 章・19 章のテスト例（提出済みの注文を再提出すると AlreadySubmitted を返す）を
// 動かすための最小限のサービス実装です。
public sealed class OrderSubmissionService
{
    private readonly HashSet<string> _submittedOrderIds = new HashSet<string>();
    private readonly decimal _creditLimit;

    public OrderSubmissionService(decimal creditLimit)
    {
        _creditLimit = creditLimit;
    }

    public SubmitOrderResult Submit(OrderId orderId, decimal amount)
    {
        if (orderId == null) throw new ArgumentNullException(nameof(orderId));

        if (amount <= 0)
        {
            return SubmitOrderResult.Invalid("注文金額は 0 より大きい必要があります。");
        }

        if (amount > _creditLimit)
        {
            return SubmitOrderResult.OverCreditLimit(_creditLimit);
        }

        if (!_submittedOrderIds.Add(orderId.Value))
        {
            return SubmitOrderResult.DuplicateSubmission(orderId);
        }

        return SubmitOrderResult.Success(orderId);
    }
}
