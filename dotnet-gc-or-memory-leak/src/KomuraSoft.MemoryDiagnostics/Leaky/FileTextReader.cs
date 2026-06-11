namespace KomuraSoft.MemoryDiagnostics.Leaky;

/// <summary>
/// 所有権が曖昧な IDisposable の例（記事 11.5 章）。
/// この例では StreamReader が stream を閉じるので大きな問題にならないことが多いですが、
/// 所有権が曖昧なコードでは漏れが起きます。
/// </summary>
public sealed class FileTextReader
{
    public async Task<string> ReadAsync(string path)
    {
        var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
