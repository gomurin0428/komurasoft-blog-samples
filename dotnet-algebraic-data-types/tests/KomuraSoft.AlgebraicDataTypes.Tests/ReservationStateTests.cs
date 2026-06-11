using KomuraSoft.AlgebraicDataTypes.Reservations;
using Xunit;

namespace KomuraSoft.AlgebraicDataTypes.Tests;

// 記事 15 章: 状態ごとに必要なデータを分ける
public class ReservationStateTests
{
    private static readonly DateTimeOffset TestTime =
        new DateTimeOffset(2026, 6, 9, 10, 0, 0, TimeSpan.FromHours(9));

    [Fact]
    public void Requestedは申込日時だけを持つ()
    {
        var state = new ReservationState.Requested(TestTime);

        Assert.Equal(TestTime, state.RequestedAt);
    }

    [Fact]
    public void Confirmedは確定日時だけを持つ()
    {
        var state = new ReservationState.Confirmed(TestTime);

        Assert.Equal(TestTime, state.ConfirmedAt);
    }

    [Fact]
    public void Cancelledだけがキャンセル日時と理由を持つ()
    {
        var state = new ReservationState.Cancelled(TestTime, "顧客都合");

        Assert.Equal(TestTime, state.CancelledAt);
        Assert.Equal("顧客都合", state.Reason);
    }

    [Fact]
    public void すべてのケースはReservationStateとして扱える()
    {
        ReservationState[] states =
        [
            new ReservationState.Requested(TestTime),
            new ReservationState.Confirmed(TestTime),
            new ReservationState.Cancelled(TestTime, "顧客都合"),
        ];

        Assert.All(states, state => Assert.IsAssignableFrom<ReservationState>(state));
    }
}
