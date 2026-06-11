namespace KomuraSoft.AlgebraicDataTypes.Records;

// 記事 14 章: 変換処理で、ADT のケースごとに DTO を作る
public static class PaymentResultMapper
{
    public static PaymentResultDto ToDto(PaymentResult result)
    {
        return result switch
        {
            PaymentResult.Succeeded x => new PaymentResultDto
            {
                Type = "succeeded",
                ReceiptNo = x.ReceiptNo
            },

            PaymentResult.Rejected x => new PaymentResultDto
            {
                Type = "rejected",
                Reason = x.Reason
            },

            PaymentResult.NetworkFailure x => new PaymentResultDto
            {
                Type = "network_failure",
                Message = x.Message
            },

            _ => throw new InvalidOperationException("未知の決済結果です。")
        };
    }
}
