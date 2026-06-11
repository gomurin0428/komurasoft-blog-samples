using System.Text;
using Xunit;

namespace KomuraSoft.FileIntegration.Tests;

public class FileTransferReceiverTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly ProcessedLedger _ledger;
    private readonly List<string> _processedPaths = [];

    public FileTransferReceiverTests()
    {
        _ledger = new ProcessedLedger(_temp.Combine("ledger"));
    }

    public void Dispose() => _temp.Dispose();

    private string IncomingDir => _temp.Combine("incoming");
    private string ArchiveDir => _temp.Combine("archive");
    private string ErrorDir => _temp.Combine("error");

    private FileTransferReceiver CreateReceiver(string workerName)
        => new(
            incomingDir: IncomingDir,
            processingDir: _temp.Combine("processing", workerName),
            archiveDir: ArchiveDir,
            errorDir: ErrorDir,
            ledger: _ledger,
            process: path =>
            {
                lock (_processedPaths)
                {
                    _processedPaths.Add(path);
                }
            });

    private static void PublishCsv(string incomingDir, string baseName, string key)
        => FileTransferSender.PublishBundle(
            incomingDir, baseName, key,
            stream => stream.Write(Encoding.UTF8.GetBytes($"payload of {baseName}\n")));

    [Fact]
    public void ProcessBundle_HappyPath_ProcessesAndArchives()
    {
        PublishCsv(IncomingDir, "a.csv", "key-a");
        FileTransferReceiver receiver = CreateReceiver("w1");

        Assert.Equal(["a.csv"], receiver.ListReadyBaseNames());
        ReceiveResult result = receiver.ProcessBundle("a.csv");

        Assert.Equal(ReceiveResult.Processed, result);
        Assert.Single(_processedPaths);
        Assert.True(_ledger.AlreadyProcessed("key-a"));
        Assert.True(File.Exists(Path.Combine(ArchiveDir, "a.csv")));
        Assert.True(File.Exists(Path.Combine(ArchiveDir, "a.csv.done")));
        Assert.Empty(Directory.GetFiles(IncomingDir));
    }

    [Fact]
    public async Task ProcessBundle_TwoWorkersRace_FileIsProcessedExactlyOnce()
    {
        // 記事 2.2 章の競合を、claim rename を入れた受信側で再現する
        PublishCsv(IncomingDir, "contended.csv", "key-c");
        FileTransferReceiver worker1 = CreateReceiver("w1");
        FileTransferReceiver worker2 = CreateReceiver("w2");

        using var start = new ManualResetEventSlim(false);
        Task<ReceiveResult> attempt1 = Task.Run(() =>
        {
            start.Wait();
            return worker1.ProcessBundle("contended.csv");
        });
        Task<ReceiveResult> attempt2 = Task.Run(() =>
        {
            start.Wait();
            return worker2.ProcessBundle("contended.csv");
        });

        start.Set();
        ReceiveResult[] results = await Task.WhenAll(attempt1, attempt2);

        Assert.Equal(1, results.Count(r => r == ReceiveResult.Processed));
        Assert.Equal(1, results.Count(r => r == ReceiveResult.ClaimedByOther));
        Assert.Single(_processedPaths); // 二重処理されない
    }

    [Fact]
    public void ProcessBundle_DuplicateIdempotencyKey_IsSkippedWithoutReprocessing()
    {
        // 同じ連携 ID のバンドルが二重に届いた（リトライや再送で普通に起きる）
        PublishCsv(IncomingDir, "first.csv", "key-dup");
        FileTransferReceiver receiver = CreateReceiver("w1");
        receiver.ProcessBundle("first.csv");

        PublishCsv(IncomingDir, "second.csv", "key-dup");
        ReceiveResult result = receiver.ProcessBundle("second.csv");

        Assert.Equal(ReceiveResult.SkippedAsDuplicate, result);
        Assert.Single(_processedPaths); // 処理は 1 回だけ（二重計上されない）
        Assert.True(File.Exists(Path.Combine(ArchiveDir, "second.csv"))); // 入力は archive に残る
    }

    [Fact]
    public void ProcessBundle_CorruptedPayload_GoesToErrorWithoutProcessing()
    {
        PublishCsv(IncomingDir, "broken.csv", "key-b");

        // 公開後に payload が壊れた状況（転送途中の破損など）を再現する
        File.WriteAllText(Path.Combine(IncomingDir, "broken.csv"), "corrupted!");

        FileTransferReceiver receiver = CreateReceiver("w1");
        ReceiveResult result = receiver.ProcessBundle("broken.csv");

        Assert.Equal(ReceiveResult.MovedToError, result);
        Assert.Empty(_processedPaths);                    // 壊れた入力は処理しない
        Assert.False(_ledger.AlreadyProcessed("key-b"));  // 処理済み扱いにもしない
        Assert.True(File.Exists(Path.Combine(ErrorDir, "broken.csv")));
        Assert.True(File.Exists(Path.Combine(ErrorDir, "broken.csv.done")));
    }
}
