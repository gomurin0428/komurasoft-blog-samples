using System.Text;
using Xunit;

namespace KomuraSoft.FileWatching.Tests;

public class BundleScannerTests
{
    [Fact]
    public void EnumerateReadyBundles_ReturnsOnlyCompletedBundles()
    {
        using var dir = new TestDirectory();
        Directory.CreateDirectory(dir.IncomingDir);

        // 完成した bundle（manifest あり、final 名）
        BundleWriter.WriteBundle(
            dir.IncomingDir,
            bundleName: "order-001",
            idempotencyKey: "order-001",
            files: new Dictionary<string, byte[]> { ["payload.csv"] = "a,b\n"u8.ToArray() });

        // 書き込み途中（temp 名のまま）
        string tempBundle = Path.Combine(dir.IncomingDir, "order-002.tmp");
        Directory.CreateDirectory(tempBundle);
        new BundleManifest("order-002").WriteToFile(Path.Combine(tempBundle, BundleManifest.FileName));

        // manifest がまだ無い（完了が明示されていない）
        string noManifest = Path.Combine(dir.IncomingDir, "order-003");
        Directory.CreateDirectory(noManifest);
        File.WriteAllText(Path.Combine(noManifest, "payload.csv"), "x,y\n");

        var bundles = BundleScanner.EnumerateReadyBundles(dir.IncomingDir, full: true).ToList();

        var bundle = Assert.Single(bundles);
        Assert.Equal("order-001", bundle.Name);
        Assert.Equal(Path.Combine(dir.IncomingDir, "order-001"), bundle.Path);
    }

    [Fact]
    public void EnumerateReadyBundles_ReturnsEmpty_WhenIncomingDirDoesNotExist()
    {
        using var dir = new TestDirectory();

        var bundles = BundleScanner.EnumerateReadyBundles(
            Path.Combine(dir.Root, "missing"),
            full: false);

        Assert.Empty(bundles);
    }

    [Fact]
    public void WriteBundle_LeavesNoTempDirectoryBehind()
    {
        using var dir = new TestDirectory();
        Directory.CreateDirectory(dir.IncomingDir);

        string finalDir = BundleWriter.WriteBundle(
            dir.IncomingDir,
            bundleName: "order-010",
            idempotencyKey: "order-010",
            files: new Dictionary<string, byte[]>
            {
                ["payload.csv"] = Encoding.UTF8.GetBytes("item,qty\napple,3\n"),
                ["note.txt"] = Encoding.UTF8.GetBytes("memo"),
            });

        Assert.Equal(Path.Combine(dir.IncomingDir, "order-010"), finalDir);
        Assert.True(File.Exists(Path.Combine(finalDir, "payload.csv")));
        Assert.True(File.Exists(Path.Combine(finalDir, "note.txt")));
        Assert.True(File.Exists(Path.Combine(finalDir, BundleManifest.FileName)));
        Assert.False(Directory.Exists(Path.Combine(dir.IncomingDir, "order-010.tmp")));
    }
}
