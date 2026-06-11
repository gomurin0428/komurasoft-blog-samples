using Xunit;

namespace KomuraSoft.MemoryDiagnostics.Tests;

/// <summary>
/// 無制限キャッシュ（記事 11.2 章）と singleton バッファ（記事 11.7 章）が、
/// キーや呼び出し回数に比例して増え続けることを確認するテストです。
/// </summary>
public class UnboundedGrowthTests
{
    [Fact]
    public void ReportCache_ReturnsSameInstance_ForSameKey()
    {
        var cache = new Leaky.ReportCache();
        var date = new DateTime(2026, 6, 9);

        Report first = cache.GetOrCreate("user-1", date);
        Report second = cache.GetOrCreate("user-1", date);

        // 同じキーなら同じインスタンス（キャッシュとしては正常な動き）
        Assert.Same(first, second);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void ReportCache_GrowsWithoutBound_WhenKeyContainsTimestamp()
    {
        var cache = new Leaky.ReportCache();

        // キーに現在時刻のような毎回違う値を含めると、呼ぶたびにエントリが増える
        for (int i = 0; i < 100; i++)
        {
            cache.GetOrCreate("user-1", new DateTime(2026, 6, 9).AddTicks(i));
        }

        Assert.Equal(100, cache.Count);
    }

    [Fact]
    public void AuditBuffer_GrowsForever_WhenUsedAsSingleton()
    {
        var buffer = new Leaky.AuditBuffer(); // singleton 登録を想定

        for (int i = 0; i < 100; i++)
        {
            buffer.Add(new RequestAudit($"/api/orders/{i}", DateTimeOffset.UtcNow));
        }

        // 上限・送信・削除がないので、リクエスト数に比例して増え続ける
        Assert.Equal(100, buffer.Count);
    }
}
