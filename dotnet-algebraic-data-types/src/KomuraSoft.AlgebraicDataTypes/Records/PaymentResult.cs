namespace KomuraSoft.AlgebraicDataTypes.Records;

// 記事 14 章: API境界ではDTOに変換する ── ドメイン内部の ADT
public abstract record PaymentResult
{
    public sealed record Succeeded(string ReceiptNo) : PaymentResult;
    public sealed record Rejected(string Reason) : PaymentResult;
    public sealed record NetworkFailure(string Message) : PaymentResult;
}
