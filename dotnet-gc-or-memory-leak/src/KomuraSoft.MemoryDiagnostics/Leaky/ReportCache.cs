namespace KomuraSoft.MemoryDiagnostics.Leaky;

/// <summary>
/// 上限も期限もないキャッシュ（記事 11.2 章）。
/// userId と date の組み合わせが増え続ければ、キャッシュも増え続けます。
/// キーに現在時刻やリクエスト ID を含めると、実質的なメモリリークになります。
/// </summary>
public sealed class ReportCache
{
    private readonly Dictionary<string, Report> _cache = new();

    public Report GetOrCreate(string userId, DateTime date)
    {
        var key = $"{userId}:{date:O}";

        if (_cache.TryGetValue(key, out var report))
        {
            return report;
        }

        report = BuildReport(userId, date);
        _cache[key] = report;
        return report;
    }

    /// <summary>キャッシュ件数（観測用）。</summary>
    public int Count => _cache.Count;

    private static Report BuildReport(string userId, DateTime date)
    {
        return new Report(userId, date);
    }
}
