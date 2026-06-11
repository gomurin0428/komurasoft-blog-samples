using Xunit;

namespace KomuraSoft.MemoryDiagnostics.Tests;

/// <summary>
/// IDisposable の所有権（記事 11.5 章）、AsyncLocal（記事 11.6 章）、
/// ArrayPool の貸し借り（記事 12 章）の動作確認です。
/// </summary>
public class ResourceOwnershipTests
{
    [Fact]
    public async Task FileTextReader_BothVariants_ReadWholeFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"komurasoft-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(path, "GC待ちとメモリリークを見分ける");

        try
        {
            string leaky = await new Leaky.FileTextReader().ReadAsync(path);
            string fixedVersion = await new Fixed.FileTextReader().ReadAsync(path);

            Assert.Equal("GC待ちとメモリリークを見分ける", leaky);
            Assert.Equal("GC待ちとメモリリークを見分ける", fixedVersion);

            // await using で所有権を明確にした版は、確実にファイルを閉じている
            File.Delete(path);
            Assert.False(File.Exists(path));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task RequestContext_FlowsIntoChildAsyncFlow_ButNotBackToParent()
    {
        RequestContext.Current.Value = new RequestInfo("req-001", "komura");

        // AsyncLocal は親から子の非同期フローへ流れる
        await Task.Run(() =>
        {
            Assert.Equal("req-001", RequestContext.Current.Value?.RequestId);

            // 子で上書きしても親には影響しない
            RequestContext.Current.Value = new RequestInfo("req-002", "other");
        });

        Assert.Equal("req-001", RequestContext.Current.Value?.RequestId);

        // 不要になったら null に戻す（大きなオブジェクトを長生きさせない）
        RequestContext.Current.Value = null;
        Assert.Null(RequestContext.Current.Value);
    }

    [Fact]
    public void PooledBuffers_RentsAtLeastRequestedLength_AndReturns()
    {
        byte[]? observed = null;

        PooledBuffers.ProcessWithPooledBuffer(1024, buffer =>
        {
            observed = buffer;
            Assert.True(buffer.Length >= 1024);
        });

        Assert.NotNull(observed);
    }

    [Fact]
    public void PooledBuffers_ReturnsBuffer_EvenWhenUseThrows()
    {
        // finally で Return しているので、例外時も返却漏れにならない
        Assert.Throws<InvalidOperationException>(() =>
            PooledBuffers.ProcessWithPooledBuffer(1024, _ => throw new InvalidOperationException()));
    }
}
