using System.Net;
using System.Text;
using Xunit;

namespace KomuraSoft.AsyncPatterns.Tests;

public class HttpDownloaderTests
{
    [Fact]
    public async Task DownloadAllAsync_StartsAllTasksBeforeAwaiting_AndReturnsInOrder()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Register("https://example.test/a", "A", TimeSpan.FromMilliseconds(100));
        handler.Register("https://example.test/b", "B", TimeSpan.FromMilliseconds(100));
        handler.Register("https://example.test/c", "C", TimeSpan.FromMilliseconds(100));
        using var httpClient = new HttpClient(handler);
        var downloader = new HttpDownloader(httpClient);

        string[] results = await downloader.DownloadAllAsync(
            ["https://example.test/a", "https://example.test/b", "https://example.test/c"],
            CancellationToken.None);

        Assert.Equal(new[] { "A", "B", "C" }, results);

        // ToArray() で確定してから WhenAll しているので、3 件が並行に走っている
        Assert.True(
            handler.MaxObservedConcurrency >= 2,
            $"Expected concurrent requests, but max concurrency was {handler.MaxObservedConcurrency}.");
    }

    [Fact]
    public async Task DownloadFromFirstMirrorAsync_ReturnsFastestMirror()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Register("https://mirror1.test/file", "slow-1", TimeSpan.FromSeconds(5));
        handler.Register("https://mirror2.test/file", "fast", TimeSpan.FromMilliseconds(10));
        handler.Register("https://mirror3.test/file", "slow-3", TimeSpan.FromSeconds(5));
        using var httpClient = new HttpClient(handler);
        var downloader = new HttpDownloader(httpClient);

        // 敗者のキャンセル回収まで含めて、遅いミラーの 5 秒を待たずに返ることを確認する
        Task<byte[]> download = downloader.DownloadFromFirstMirrorAsync(
            ["https://mirror1.test/file", "https://mirror2.test/file", "https://mirror3.test/file"],
            CancellationToken.None);
        byte[] result = await download.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Equal("fast", Encoding.UTF8.GetString(result));
    }

    [Fact]
    public async Task DownloadAndSaveAsync_SavesAllFiles_WithBoundedParallelism()
    {
        var handler = new FakeHttpMessageHandler();
        string[] urls = Enumerable.Range(0, 20)
            .Select(i => $"https://example.test/page-{i}")
            .ToArray();
        foreach (string url in urls)
        {
            handler.Register(url, $"body of {url}", TimeSpan.FromMilliseconds(20));
        }

        using var httpClient = new HttpClient(handler);
        var downloader = new HttpDownloader(httpClient);

        // 保存先の "cache" は現在ディレクトリ相対なので、テスト用に用意する
        string workDir = Directory.CreateTempSubdirectory("async-patterns-test").FullName;
        string originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(workDir);
        try
        {
            Directory.CreateDirectory("cache");

            await downloader.DownloadAndSaveAsync(urls, CancellationToken.None);

            Assert.Equal(20, Directory.GetFiles("cache", "*.html").Length);
            Assert.Equal(
                "body of https://example.test/page-0",
                await File.ReadAllTextAsync(Path.Combine("cache", "0.html")));

            // MaxDegreeOfParallelism = 8 を超えて同時に走らない
            Assert.InRange(handler.MaxObservedConcurrency, 1, 8);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadTextAsync_ReturnsBody()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Register("https://example.test/text", "hello");
        using var httpClient = new HttpClient(handler);
        var downloader = new HttpDownloader(httpClient);

        string text = await downloader.DownloadTextAsync("https://example.test/text", CancellationToken.None);

        Assert.Equal("hello", text);
    }

    [Fact]
    public async Task DownloadTextAsync_Throws_WhenStatusCodeIsNotSuccess()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Register("https://example.test/error", "oops", statusCode: HttpStatusCode.InternalServerError);
        using var httpClient = new HttpClient(handler);
        var downloader = new HttpDownloader(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => downloader.DownloadTextAsync("https://example.test/error", CancellationToken.None));
    }

    [Fact]
    public async Task DownloadTextAsync_PropagatesCancellation()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Register("https://example.test/slow", "slow", TimeSpan.FromSeconds(30));
        using var httpClient = new HttpClient(handler);
        var downloader = new HttpDownloader(httpClient);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // token を下流（HttpClient）へ渡しているので、途中でちゃんと止まる
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => downloader.DownloadTextAsync("https://example.test/slow", cts.Token)
                .WaitAsync(TimeSpan.FromSeconds(5)));
    }
}
