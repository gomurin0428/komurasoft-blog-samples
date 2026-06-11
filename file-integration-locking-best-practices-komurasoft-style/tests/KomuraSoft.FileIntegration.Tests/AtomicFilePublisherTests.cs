using System.Text;
using Xunit;

namespace KomuraSoft.FileIntegration.Tests;

public class AtomicFilePublisherTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Publish_FinalNameIsNeverVisibleWhileWriting()
    {
        string finalPath = _temp.Combine("orders.csv");
        bool finalVisibleDuringWrite = false;

        AtomicFilePublisher.Publish(finalPath, stream =>
        {
            // 書き込みの途中（記事 2.1 章の事故が起きるタイミング）で final 名を覗く
            stream.Write("id,amount\n"u8);
            finalVisibleDuringWrite = File.Exists(finalPath);
            stream.Write("1,100\n"u8);
        });

        Assert.False(finalVisibleDuringWrite); // 書き込み中に final 名は見えない
        Assert.True(File.Exists(finalPath));
        Assert.Equal("id,amount\n1,100\n", File.ReadAllText(finalPath));
    }

    [Fact]
    public void Publish_LeavesNoTempFileBehind()
    {
        string finalPath = _temp.Combine("data.csv");

        AtomicFilePublisher.PublishText(finalPath, "hello");

        string[] leftovers = Directory.GetFiles(_temp.Path, "*.tmp");
        Assert.Empty(leftovers);
    }

    [Fact]
    public void Publish_ReplacesExistingFinalFile()
    {
        string finalPath = _temp.Combine("status.json");
        AtomicFilePublisher.PublishText(finalPath, "old");

        AtomicFilePublisher.PublishText(finalPath, "new");

        Assert.Equal("new", File.ReadAllText(finalPath));
    }

    [Fact]
    public void Publish_TempIsOpenedWithFileShareNone()
    {
        string finalPath = _temp.Combine("locked.csv");

        AtomicFilePublisher.Publish(finalPath, stream =>
        {
            stream.Write("partial"u8);

            // 生成中の temp ファイルは FileShare.None なので、他プロセス相当のオープンは失敗する
            string tempPath = Directory.GetFiles(_temp.Path, "*.tmp").Single();
            Assert.Throws<IOException>(() =>
                new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read));
        });
    }

    [Fact]
    public void Publish_CleansUpTempWhenWriteFails()
    {
        string finalPath = _temp.Combine("broken.csv");

        Assert.Throws<InvalidOperationException>(() =>
            AtomicFilePublisher.Publish(finalPath, _ =>
                throw new InvalidOperationException("boom")));

        Assert.False(File.Exists(finalPath));            // 失敗時に final 名は現れない
        Assert.Empty(Directory.GetFiles(_temp.Path));    // temp も残らない
    }

    [Fact]
    public void MakeTempPathSameDirectory_StaysInSameDirectory()
    {
        string finalPath = _temp.Combine("report.zip");

        string tempPath = AtomicFilePublisher.MakeTempPathSameDirectory(finalPath);

        // temp と final は同一ディレクトリ（= 同一ボリューム）に置く（記事 4.1 章）
        Assert.Equal(
            Path.GetDirectoryName(Path.GetFullPath(finalPath)),
            Path.GetDirectoryName(tempPath));
        Assert.EndsWith(".tmp", tempPath, StringComparison.Ordinal);
    }

    [Fact]
    public void PublishDoneFile_ManifestRoundTripsAndVerifies()
    {
        string finalPath = _temp.Combine("data.csv");
        byte[] payload = Encoding.UTF8.GetBytes("id,amount\n1,100\n2,250\n");
        AtomicFilePublisher.Publish(finalPath, stream => stream.Write(payload));

        var manifest = TransferManifest.CreateFor(finalPath, "integration-0001");
        AtomicFilePublisher.PublishDoneFile(finalPath + ".done", manifest);

        TransferManifest loaded = TransferManifest.ReadDoneFile(finalPath + ".done");
        Assert.Equal("data.csv", loaded.FileName);
        Assert.Equal(payload.Length, loaded.Size);
        Assert.Equal("integration-0001", loaded.IdempotencyKey);
        loaded.VerifyPayload(finalPath); // サイズ・ハッシュが一致するので例外にならない
    }

    [Fact]
    public void VerifyPayload_DetectsTamperedPayload()
    {
        string finalPath = _temp.Combine("data.csv");
        AtomicFilePublisher.PublishText(finalPath, "id,amount\n1,100\n");
        var manifest = TransferManifest.CreateFor(finalPath, "integration-0002");

        // 公開後に中身が変わってしまった（= 途中書き込みや破損の検出と同じ仕組み）
        File.WriteAllText(finalPath, "id,amount\n1,999\n");

        Assert.Throws<InvalidDataException>(() => manifest.VerifyPayload(finalPath));
    }
}
