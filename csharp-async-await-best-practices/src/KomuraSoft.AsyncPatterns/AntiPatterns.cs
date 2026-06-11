namespace KomuraSoft.AsyncPatterns;

/// <summary>
/// 記事 5 章（よくあるアンチパターン）に登場する「良くない例」をまとめたクラスです。
/// どれもデッドロックやハングはしませんが、実務ではこう書かないでください。
/// それぞれの置き換え先は、対応する良い例（FileTextLoader / HttpDownloader）を参照してください。
/// </summary>
public sealed class AntiPatterns
{
    private readonly HttpClient _httpClient;

    public AntiPatterns(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    /// <summary>
    /// 良くない例（記事 3.2）: すでに async な I/O を Task.Run で包んでいます。
    /// I/O 待ちを別スレッドへ投げ直しているだけで、整理しづらくなるわりに得がありません。
    /// 置き換え先: <see cref="FileTextLoader.LoadTextAsync"/>（plain await）。
    /// </summary>
    public async Task<string> LoadTextAsync(string path, CancellationToken cancellationToken)
    {
        return await Task.Run(() => File.ReadAllTextAsync(path, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// 良くない例（記事 3.4）: 互いに独立した処理を 1 件ずつ直列に await しています。
    /// 不必要に遅くなります。
    /// 置き換え先: <see cref="HttpDownloader.DownloadAllAsync"/>（先に全部開始して Task.WhenAll）。
    /// </summary>
    public async Task<(string A, string B, string C)> DownloadSequentiallyAsync(
        string urlA,
        string urlB,
        string urlC,
        CancellationToken cancellationToken)
    {
        // 独立しているのに直列になっている例
        string a = await _httpClient.GetStringAsync(urlA, cancellationToken);
        string b = await _httpClient.GetStringAsync(urlB, cancellationToken);
        string c = await _httpClient.GetStringAsync(urlC, cancellationToken);

        return (a, b, c);
    }

    // このほか記事 5 章で挙げているアンチパターン（Task.Result / Wait() によるブロッキング、
    // async フローへの Thread.Sleep 混入、イベントハンドラ以外での async void、
    // lock で await をまたぐ、ValueTask の常用など）は、実行するとスレッドを塞いだり
    // デッドロックの温床になったりするため、このサンプルではコードとして収録していません。
    // 置き換え先は記事 5 章の表を参照してください。
}
