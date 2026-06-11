using Xunit;

namespace KomuraSoft.FileWatching.Tests;

public class BundleManifestTests
{
    [Fact]
    public void WriteToFile_ThenReadFromFile_RoundTrips()
    {
        using var dir = new TestDirectory();
        string manifestPath = Path.Combine(dir.Root, BundleManifest.FileName);

        new BundleManifest("order-123").WriteToFile(manifestPath);
        BundleManifest manifest = BundleManifest.ReadFromFile(manifestPath);

        Assert.Equal("order-123", manifest.IdempotencyKey);
    }

    [Fact]
    public void ReadFromFile_IsCaseInsensitiveForPropertyNames()
    {
        using var dir = new TestDirectory();
        string manifestPath = Path.Combine(dir.Root, BundleManifest.FileName);
        File.WriteAllText(manifestPath, """{ "idempotencyKey": "order-456" }""");

        BundleManifest manifest = BundleManifest.ReadFromFile(manifestPath);

        Assert.Equal("order-456", manifest.IdempotencyKey);
    }
}
