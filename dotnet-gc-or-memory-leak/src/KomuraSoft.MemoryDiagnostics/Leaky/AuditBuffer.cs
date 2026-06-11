namespace KomuraSoft.MemoryDiagnostics.Leaky;

/// <summary>
/// DI ライフタイムの取り違え（記事 11.7 章）。
/// これが singleton として登録されると、_items の寿命はアプリの寿命と同じになり、
/// リクエストごとのデータが溜まり続けます。
/// </summary>
public sealed class AuditBuffer
{
    private readonly List<RequestAudit> _items = new();

    public void Add(RequestAudit item)
    {
        _items.Add(item);
    }

    /// <summary>保持件数（観測用）。</summary>
    public int Count => _items.Count;
}
