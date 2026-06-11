namespace KomuraSoft.AlgebraicDataTypes.Reservations;

// 記事 15 章: 不正な状態を作りにくくなる
// キャンセル済み状態だけがキャンセル日時と理由を持つ
public abstract class ReservationState
{
    private ReservationState()
    {
    }

    public sealed class Requested : ReservationState
    {
        internal Requested(DateTimeOffset requestedAt)
        {
            RequestedAt = requestedAt;
        }

        public DateTimeOffset RequestedAt { get; }
    }

    public sealed class Confirmed : ReservationState
    {
        internal Confirmed(DateTimeOffset confirmedAt)
        {
            ConfirmedAt = confirmedAt;
        }

        public DateTimeOffset ConfirmedAt { get; }
    }

    public sealed class Cancelled : ReservationState
    {
        internal Cancelled(DateTimeOffset cancelledAt, string reason)
        {
            CancelledAt = cancelledAt;
            Reason = reason;
        }

        public DateTimeOffset CancelledAt { get; }
        public string Reason { get; }
    }
}
