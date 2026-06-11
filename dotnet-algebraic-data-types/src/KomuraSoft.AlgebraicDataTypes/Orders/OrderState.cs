namespace KomuraSoft.AlgebraicDataTypes.Orders;

// 記事 13 章: 状態遷移を型で表す
// 状態ごとに必要なデータだけを持たせる
public abstract class OrderState
{
    private OrderState()
    {
    }

    public sealed class Draft : OrderState
    {
        internal Draft(UserId createdBy)
        {
            CreatedBy = createdBy;
        }

        public UserId CreatedBy { get; }
    }

    public sealed class Submitted : OrderState
    {
        internal Submitted(DateTimeOffset submittedAt)
        {
            SubmittedAt = submittedAt;
        }

        public DateTimeOffset SubmittedAt { get; }
    }

    public sealed class Paid : OrderState
    {
        internal Paid(string paymentNo)
        {
            PaymentNo = paymentNo;
        }

        public string PaymentNo { get; }
    }

    public sealed class Shipped : OrderState
    {
        internal Shipped(string trackingNo)
        {
            TrackingNo = trackingNo;
        }

        public string TrackingNo { get; }
    }

    public sealed class Cancelled : OrderState
    {
        internal Cancelled(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }
}
