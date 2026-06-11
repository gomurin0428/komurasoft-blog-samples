namespace KomuraSoft.FileWatching.Tests;

/// <summary>
/// テストごとに使い捨てる一時ディレクトリです。
/// </summary>
public sealed class TestDirectory : IDisposable
{
    public string Root { get; } =
        Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), $"fsw-tests-{Guid.NewGuid():N}")).FullName;

    public string IncomingDir => Path.Combine(Root, "incoming");
    public string ArchiveDir => Path.Combine(Root, "archive");
    public string ProcessedDir => Path.Combine(Root, "processed");

    public string ProcessingDir(string worker) => Path.Combine(Root, "processing", worker);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }
}
