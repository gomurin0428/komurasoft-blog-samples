namespace KomuraSoft.AsyncPatterns;

/// <summary>
/// HttpClient を使った各パターンの実装です。
/// - Task.WhenAll（記事 3.4）
/// - Task.WhenAny（記事 3.5）
/// - Parallel.ForEachAsync（記事 3.6）
/// - CancellationToken の伝播（記事 4.3）
/// </summary>
public sealed class HttpDownloader
{
    private readonly HttpClient _httpClient;

    public HttpDownloader(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    /// <summary>
    /// 独立した複数処理を、先に全部開始してからまとめて待ちます（記事 3.4）。
    /// LINQ は遅延実行なので、ToArray() でいったん確定して全タスクを開始するのがポイントです（記事 4.5）。
    /// </summary>
    public async Task<string[]> DownloadAllAsync(IEnumerable<string> urls, CancellationToken cancellationToken)
    {
        Task<string>[] tasks = urls
            .Select(url => _httpClient.GetStringAsync(url, cancellationToken))
            .ToArray();

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 複数のミラー先のうち、最初に応答したものを使います（記事 3.5）。
    /// WhenAny は勝者を 1 つ返すだけなので、残りのキャンセルと例外の回収まで書きます。
    /// </summary>
    public async Task<byte[]> DownloadFromFirstMirrorAsync(
        IReadOnlyList<string> urls,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Task<byte[]>[] tasks = urls
            .Select(url => _httpClient.GetByteArrayAsync(url, cts.Token))
            .ToArray();

        Task<byte[]> winner = await Task.WhenAny(tasks);
        cts.Cancel();

        try
        {
            return await winner;
        }
        finally
        {
            try
            {
                await Task.WhenAll(tasks);
            }
            catch
            {
                // 勝者以外のキャンセルや失敗を回収する
            }
        }
    }

    /// <summary>
    /// 件数が多いときは、並列数の上限を明示して回します（記事 3.6）。
    /// 保存先の "cache" ディレクトリは、呼び出し側であらかじめ作成しておきます。
    /// </summary>
    public async Task DownloadAndSaveAsync(IEnumerable<string> urls, CancellationToken cancellationToken)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 8,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(
            urls.Select((url, index) => (url, index)),
            options,
            async (item, token) =>
            {
                string html = await _httpClient.GetStringAsync(item.url, token);
                string path = Path.Combine("cache", $"{item.index}.html");
                await File.WriteAllTextAsync(path, html, token);
            });
    }

    /// <summary>
    /// CancellationToken を受けて、そのまま下流へ渡します（記事 4.3）。
    /// </summary>
    public async Task<string> DownloadTextAsync(string url, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
