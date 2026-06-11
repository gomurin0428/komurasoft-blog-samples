namespace KomuraSoft.FileIntegration.Tests;

/// <summary>テストごとに使い捨ての作業ディレクトリを用意します。</summary>
public sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "komurasoft-file-integration-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Combine(params string[] parts)
        => System.IO.Path.Combine([Path, .. parts]);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // テストの後始末失敗でテスト自体は落とさない
        }
    }
}
