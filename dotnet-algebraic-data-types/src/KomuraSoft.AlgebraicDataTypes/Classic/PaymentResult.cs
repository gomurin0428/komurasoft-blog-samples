namespace KomuraSoft.AlgebraicDataTypes.Classic;

// 記事 5 章: private コンストラクターで閉じた集合にする
// ネストした型だけが基底クラスを継承できる
public abstract class PaymentResult
{
    private PaymentResult()
    {
    }

    public sealed class Succeeded : PaymentResult
    {
        internal Succeeded(string receiptNo)
        {
            ReceiptNo = receiptNo;
        }

        public string ReceiptNo { get; }
    }

    public sealed class InsufficientFunds : PaymentResult
    {
        internal InsufficientFunds(decimal shortage)
        {
            Shortage = shortage;
        }

        public decimal Shortage { get; }
    }

    public static PaymentResult Success(string receiptNo)
        => new Succeeded(receiptNo);

    public static PaymentResult Insufficient(decimal shortage)
        => new InsufficientFunds(shortage);
}
