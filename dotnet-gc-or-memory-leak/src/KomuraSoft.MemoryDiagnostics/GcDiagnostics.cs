namespace KomuraSoft.MemoryDiagnostics;

/// <summary>
/// アプリ側に入れておく簡単な GC 診断スナップショット（記事 17 章）。
/// この情報だけでリーク判定はできませんが、障害時の傾向判断に役立ちます。
/// </summary>
public static class GcDiagnostics
{
    public static object Snapshot()
    {
        var info = GC.GetGCMemoryInfo();

        return new
        {
            TotalMemory = GC.GetTotalMemory(forceFullCollection: false),
            HeapSizeBytes = info.HeapSizeBytes,
            FragmentedBytes = info.FragmentedBytes,
            MemoryLoadBytes = info.MemoryLoadBytes,
            HighMemoryLoadThresholdBytes = info.HighMemoryLoadThresholdBytes,
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        };
    }
}
