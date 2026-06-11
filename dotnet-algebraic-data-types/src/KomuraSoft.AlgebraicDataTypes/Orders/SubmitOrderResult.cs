namespace KomuraSoft.AlgebraicDataTypes.Orders;

// 記事 17 章・19 章: 注文提出の結果型
// SubmitOrderResult = Submitted / AlreadySubmitted / InvalidOrder / CreditLimitExceeded
public abstract class SubmitOrderResult
{
    private SubmitOrderResult()
    {
    }

    public sealed class Submitted : SubmitOrderResult
    {
        internal Submitted(OrderId orderId)
        {
            OrderId = orderId;
        }

        public OrderId OrderId { get; }
    }

    public sealed class AlreadySubmitted : SubmitOrderResult
    {
        internal AlreadySubmitted(OrderId orderId)
        {
            OrderId = orderId;
        }

        public OrderId OrderId { get; }
    }

    public sealed class InvalidOrder : SubmitOrderResult
    {
        internal InvalidOrder(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }

    public sealed class CreditLimitExceeded : SubmitOrderResult
    {
        internal CreditLimitExceeded(decimal limit)
        {
            Limit = limit;
        }

        public decimal Limit { get; }
    }

    public static SubmitOrderResult Success(OrderId orderId)
        => new Submitted(orderId);

    public static SubmitOrderResult DuplicateSubmission(OrderId orderId)
        => new AlreadySubmitted(orderId);

    public static SubmitOrderResult Invalid(string reason)
        => new InvalidOrder(reason);

    public static SubmitOrderResult OverCreditLimit(decimal limit)
        => new CreditLimitExceeded(limit);

    public T Match<T>(
        Func<Submitted, T> submitted,
        Func<AlreadySubmitted, T> alreadySubmitted,
        Func<InvalidOrder, T> invalidOrder,
        Func<CreditLimitExceeded, T> creditLimitExceeded)
    {
        if (submitted == null) throw new ArgumentNullException(nameof(submitted));
        if (alreadySubmitted == null) throw new ArgumentNullException(nameof(alreadySubmitted));
        if (invalidOrder == null) throw new ArgumentNullException(nameof(invalidOrder));
        if (creditLimitExceeded == null) throw new ArgumentNullException(nameof(creditLimitExceeded));

        var s = this as Submitted;
        if (s != null) return submitted(s);

        var a = this as AlreadySubmitted;
        if (a != null) return alreadySubmitted(a);

        var i = this as InvalidOrder;
        if (i != null) return invalidOrder(i);

        var c = this as CreditLimitExceeded;
        if (c != null) return creditLimitExceeded(c);

        throw new InvalidOperationException("Unknown result type: " + GetType().FullName);
    }
}
