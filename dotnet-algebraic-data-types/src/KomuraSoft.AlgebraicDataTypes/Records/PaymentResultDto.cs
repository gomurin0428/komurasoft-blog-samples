namespace KomuraSoft.AlgebraicDataTypes.Records;

// 記事 14 章: 外部境界用の DTO
// JSON 側の判別子（Type）を持ち、ドメインの ADT とは別に安定させる
public sealed class PaymentResultDto
{
    public string? Type { get; set; }
    public string? ReceiptNo { get; set; }
    public string? Reason { get; set; }
    public string? Message { get; set; }
}
