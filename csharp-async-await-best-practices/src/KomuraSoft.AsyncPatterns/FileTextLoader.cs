namespace KomuraSoft.AsyncPatterns;

/// <summary>
/// I/O 待ちなら async API をそのまま await する例です（記事 3.2）。
/// </summary>
public static class FileTextLoader
{
    /// <summary>
    /// async 版 API（File.ReadAllTextAsync）をそのまま await する基本形。
    /// token を受けたら、そのまま下流へ渡します。
    /// </summary>
    public static async Task<string> LoadTextAsync(string path, CancellationToken cancellationToken)
    {
        return await File.ReadAllTextAsync(path, cancellationToken);
    }
}
