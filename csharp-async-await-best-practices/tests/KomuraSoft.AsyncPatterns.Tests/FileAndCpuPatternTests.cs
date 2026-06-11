using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace KomuraSoft.AsyncPatterns.Tests;

public class FileAndCpuPatternTests
{
    [Fact]
    public async Task LoadTextAsync_ReadsFileContent()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            await File.WriteAllTextAsync(path, "こんにちは、async/await。");

            string text = await FileTextLoader.LoadTextAsync(path, CancellationToken.None);

            Assert.Equal("こんにちは、async/await。", text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task HashManyTimesAsync_MatchesRepeatedSha256()
    {
        byte[] data = Encoding.UTF8.GetBytes("komura");

        byte[] expected = data;
        for (int i = 0; i < 3; i++)
        {
            expected = SHA256.HashData(expected);
        }

        byte[] actual = await CpuBoundHasher.HashManyTimesAsync(data, repeat: 3, CancellationToken.None);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task HashManyTimesAsync_ThrowsWhenAlreadyCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CpuBoundHasher.HashManyTimesAsync([1, 2, 3], repeat: 10, cts.Token));
    }

    [Fact]
    public async Task WriteFileAsync_WritesAllBytes()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        byte[] data = Encoding.UTF8.GetBytes("await using で破棄する");
        try
        {
            await AsyncFileWriter.WriteFileAsync(path, data, CancellationToken.None);

            Assert.Equal(data, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task AntiPattern_LoadTextAsync_StillReturnsContent_ButIsJustWastedScheduling()
    {
        // 「良くない例」も動きはする（だから気付きにくい）ことの確認。
        // I/O 待ちを Task.Run で包んでも結果は同じで、スケジューリングが増えるだけ。
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        using var httpClient = new HttpClient(new FakeHttpMessageHandler());
        var antiPatterns = new AntiPatterns(httpClient);
        try
        {
            await File.WriteAllTextAsync(path, "same result");

            string text = await antiPatterns.LoadTextAsync(path, CancellationToken.None);

            Assert.Equal("same result", text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task AntiPattern_DownloadSequentiallyAsync_NeverOverlapsRequests()
    {
        // 直列 await では同時実行数が 1 のまま（= 不必要に遅い）ことを確認する
        var handler = new FakeHttpMessageHandler();
        handler.Register("https://example.test/a", "A", TimeSpan.FromMilliseconds(30));
        handler.Register("https://example.test/b", "B", TimeSpan.FromMilliseconds(30));
        handler.Register("https://example.test/c", "C", TimeSpan.FromMilliseconds(30));
        using var httpClient = new HttpClient(handler);
        var antiPatterns = new AntiPatterns(httpClient);

        (string a, string b, string c) = await antiPatterns.DownloadSequentiallyAsync(
            "https://example.test/a",
            "https://example.test/b",
            "https://example.test/c",
            CancellationToken.None);

        Assert.Equal(("A", "B", "C"), (a, b, c));
        Assert.Equal(1, handler.MaxObservedConcurrency);
    }
}
