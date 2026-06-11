using Xunit;

namespace KomuraSoft.FileIntegration.Tests;

public class FileClaimerTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    private string IncomingDir => _temp.Combine("incoming");
    private string ProcessingDir(string worker) => _temp.Combine("processing", worker);

    private void PublishBundle(string baseName, string key = "key-1")
        => FileTransferSender.PublishBundle(
            IncomingDir, baseName, key,
            stream => stream.Write("payload\n"u8));

    [Fact]
    public void TryClaimBundleByRename_OnlyFirstWorkerWins()
    {
        PublishBundle("a.csv");

        bool first = FileClaimer.TryClaimBundleByRename("a.csv", IncomingDir, ProcessingDir("w1"));
        bool second = FileClaimer.TryClaimBundleByRename("a.csv", IncomingDir, ProcessingDir("w2"));

        Assert.True(first);
        Assert.False(second); // 後から来たワーカーは所有権を取れない
        Assert.True(File.Exists(Path.Combine(ProcessingDir("w1"), "a.csv")));
        Assert.True(File.Exists(Path.Combine(ProcessingDir("w1"), "a.csv.done")));
        Assert.Empty(Directory.GetFiles(IncomingDir));
    }

    [Fact]
    public async Task TryClaimBundleByRename_ExactlyOneWinnerUnderConcurrency()
    {
        // 記事 2.2 章「複数ワーカーが同じファイルを同時に拾う」を多重で再現し、
        // claim rename なら勝者が常に 1 人であることを確認する
        const int workers = 16;
        PublishBundle("contended.csv");

        using var start = new ManualResetEventSlim(false);
        Task<bool>[] attempts = Enumerable.Range(0, workers)
            .Select(i => Task.Run(() =>
            {
                start.Wait();
                return FileClaimer.TryClaimBundleByRename(
                    "contended.csv", IncomingDir, ProcessingDir($"w{i}"));
            }))
            .ToArray();

        start.Set();
        bool[] results = await Task.WhenAll(attempts);

        Assert.Equal(1, results.Count(r => r)); // 成功はちょうど 1 ワーカーだけ
    }

    [Fact]
    public void MoveBundle_MovesPayloadAndDoneFile()
    {
        PublishBundle("b.csv");
        FileClaimer.TryClaimBundleByRename("b.csv", IncomingDir, ProcessingDir("w1"));
        string archiveDir = _temp.Combine("archive");

        FileClaimer.MoveBundle(ProcessingDir("w1"), archiveDir, "b.csv");

        Assert.True(File.Exists(Path.Combine(archiveDir, "b.csv")));
        Assert.True(File.Exists(Path.Combine(archiveDir, "b.csv.done")));
        Assert.Empty(Directory.GetFiles(ProcessingDir("w1")));
    }

    [Fact]
    public void ListReadyBaseNames_ReturnsOnlyBundlesWithDoneFile()
    {
        Directory.CreateDirectory(IncomingDir);
        PublishBundle("ready.csv");

        // done file がまだ無い = 公開が完了していないので「読んでよい」状態ではない
        File.WriteAllText(Path.Combine(IncomingDir, "not-ready.csv"), "writing...");

        IReadOnlyList<string> ready = FileClaimer.ListReadyBaseNames(IncomingDir);

        Assert.Equal(["ready.csv"], ready);
    }
}
