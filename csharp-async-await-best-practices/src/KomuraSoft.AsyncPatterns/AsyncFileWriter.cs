namespace KomuraSoft.AsyncPatterns;

/// <summary>
/// IAsyncDisposable な型を await using で破棄する例です（記事 3.10）。
/// 「書き込みは async にしたのに、最後の破棄だけ同期」というずれを避けます。
/// </summary>
public static class AsyncFileWriter
{
    public static async Task WriteFileAsync(string path, byte[] data, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await stream.WriteAsync(data, cancellationToken);
    }
}
