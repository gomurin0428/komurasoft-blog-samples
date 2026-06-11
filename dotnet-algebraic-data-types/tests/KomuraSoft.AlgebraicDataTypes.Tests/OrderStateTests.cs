using KomuraSoft.AlgebraicDataTypes.Orders;
using Xunit;

namespace KomuraSoft.AlgebraicDataTypes.Tests;

// 記事 13 章: 状態遷移を型で表す
public class OrderStateTests
{
    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset now) => Now = now;
        public DateTimeOffset Now { get; }
    }

    private static readonly DateTimeOffset TestTime =
        new DateTimeOffset(2026, 6, 9, 17, 30, 0, TimeSpan.FromHours(9));

    [Fact]
    public void 作成直後の注文はDraft状態で作成者を持つ()
    {
        var createdBy = new UserId("U-0001");

        var order = new Order(new OrderId("ORD-001"), createdBy);

        var draft = Assert.IsType<OrderState.Draft>(order.State);
        Assert.Same(createdBy, draft.CreatedBy);
    }

    [Fact]
    public void Submitで提出日時を持つSubmitted状態になる()
    {
        var order = new Order(new OrderId("ORD-001"), new UserId("U-0001"));

        order.Submit(new FixedClock(TestTime));

        var submitted = Assert.IsType<OrderState.Submitted>(order.State);
        Assert.Equal(TestTime, submitted.SubmittedAt);
    }

    [Fact]
    public void MarkAsPaidで決済番号を持つPaid状態になる()
    {
        var order = new Order(new OrderId("ORD-001"), new UserId("U-0001"));
        order.Submit(new FixedClock(TestTime));

        order.MarkAsPaid("PAY-001");

        var paid = Assert.IsType<OrderState.Paid>(order.State);
        Assert.Equal("PAY-001", paid.PaymentNo);
    }

    [Fact]
    public void 下書き以外の注文は提出できない()
    {
        var order = new Order(new OrderId("ORD-001"), new UserId("U-0001"));
        order.Submit(new FixedClock(TestTime));

        Assert.Throws<InvalidOperationException>(() => order.Submit(new FixedClock(TestTime)));
    }

    [Fact]
    public void 提出前の注文は決済済みにできない()
    {
        var order = new Order(new OrderId("ORD-001"), new UserId("U-0001"));

        Assert.Throws<InvalidOperationException>(() => order.MarkAsPaid("PAY-001"));
    }
}
