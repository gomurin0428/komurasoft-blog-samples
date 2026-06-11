using Xunit;

namespace KomuraSoft.MemoryDiagnostics.Tests;

/// <summary>
/// GcDiagnostics.Snapshot（記事 17 章）と
/// ForceFullGcForDiagnosticsOnly（記事 7 章）の動作確認です。
/// </summary>
public class GcDiagnosticsTests
{
    [Fact]
    public void Snapshot_ContainsExpectedMetrics()
    {
        object snapshot = GcDiagnostics.Snapshot();
        var type = snapshot.GetType();

        string[] expectedProperties =
        [
            "TotalMemory",
            "HeapSizeBytes",
            "FragmentedBytes",
            "MemoryLoadBytes",
            "HighMemoryLoadThresholdBytes",
            "Gen0Collections",
            "Gen1Collections",
            "Gen2Collections",
        ];

        foreach (string name in expectedProperties)
        {
            Assert.NotNull(type.GetProperty(name));
        }

        long totalMemory = (long)type.GetProperty("TotalMemory")!.GetValue(snapshot)!;
        Assert.True(totalMemory > 0);
    }

    [Fact]
    public void ForceFullGc_IncrementsGen2CollectionCount()
    {
        int before = GC.CollectionCount(2);

        DiagnosticGc.ForceFullGcForDiagnosticsOnly();

        // GC.Collect() を 2 回呼ぶので、Gen 2 の回数は必ず増える
        Assert.True(GC.CollectionCount(2) > before);
    }
}
