namespace KomuraSoft.MemoryDiagnostics.Leaky;

/// <summary>
/// static コレクションによる保持（記事 11.1 章）。
/// 追加した Customer は、プロセスが生きている限り GC から「使用中」に見え続けます。
/// </summary>
public static class CustomerStore
{
    private static readonly List<Customer> Customers = new();

    public static void Add(Customer customer)
    {
        Customers.Add(customer);
    }

    /// <summary>保持件数（観測用）。</summary>
    public static int Count => Customers.Count;

    /// <summary>デモ・テストの後始末用。記事の例にはない、検証コード専用のメソッドです。</summary>
    public static void ClearForDemo()
    {
        Customers.Clear();
    }
}
