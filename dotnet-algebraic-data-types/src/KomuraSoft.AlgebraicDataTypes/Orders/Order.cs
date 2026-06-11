namespace KomuraSoft.AlgebraicDataTypes.Orders;

// 記事 13 章: 注文は OrderState を持ち、状態遷移をメソッドに閉じ込める
public sealed class Order
{
    public OrderId Id { get; }
    public OrderState State { get; private set; }

    public Order(OrderId id, UserId createdBy)
    {
        Id = id;
        State = new OrderState.Draft(createdBy);
    }

    public void Submit(IClock clock)
    {
        if (!(State is OrderState.Draft))
        {
            throw new InvalidOperationException("下書き状態の注文だけ提出できます。");
        }

        State = new OrderState.Submitted(clock.Now);
    }

    public void MarkAsPaid(string paymentNo)
    {
        if (!(State is OrderState.Submitted))
        {
            throw new InvalidOperationException("提出済みの注文だけ決済済みにできます。");
        }

        State = new OrderState.Paid(paymentNo);
    }
}
