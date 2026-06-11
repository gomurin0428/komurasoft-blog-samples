namespace KomuraSoft.MemoryDiagnostics.Fixed;

/// <summary>
/// using / await using で所有権を明確にした例（記事 11.5 章）。
/// </summary>
public sealed class FileTextReader
{
    public async Task<string> ReadAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
