using System.Text;
using KomuraSoft.FileWatching;

// FileSystemWatcher の通知を「再スキャン要求」に畳み、
// startup full rescan → 原子的 claim → idempotency 確認 → 処理 → archive
// という流れを一時ディレクトリ上で実演するデモです（記事 4 章・5.2 節）。

string root = Path.Combine(Path.GetTempPath(), $"fsw-demo-{Guid.NewGuid():N}");
string incomingDir = Path.Combine(root, "incoming");
string processingDir = Path.Combine(root, "processing", "worker1");
string archiveDir = Path.Combine(root, "archive");
string processedDir = Path.Combine(root, "processed");

Directory.CreateDirectory(incomingDir);
Console.WriteLine($"[demo] root: {root}");

// アプリ起動前から置かれていた bundle はイベントでは拾えない。
// startup full rescan で拾えることを確認するため、ワーカー起動前に 1 件置いておく（記事 4.4 節）。
BundleWriter.WriteBundle(
    incomingDir,
    bundleName: "order-001",
    idempotencyKey: "order-001",
    files: new Dictionary<string, byte[]>
    {
        ["payload.csv"] = Encoding.UTF8.GetBytes("item,qty\napple,3\n"),
    });
Console.WriteLine("[sender] order-001 を起動前に配置（startup scan で拾われるはず）");

int processedCount = 0;

using var worker = new BundleProcessingWorker(new BundleProcessingWorkerOptions
{
    IncomingDirectory = incomingDir,
    ProcessingDirectory = processingDir,
    ArchiveDirectory = archiveDir,
    ProcessedStore = new FileProcessedStore(processedDir),
    ProcessBundle = claimedPath =>
    {
        string payload = File.ReadAllText(Path.Combine(claimedPath, "payload.csv"));
        Interlocked.Increment(ref processedCount);
        Console.WriteLine($"[worker] processed {Path.GetFileName(claimedPath)}: {payload.ReplaceLineEndings(" / ").TrimEnd(' ', '/')}");
    },
    Log = exception => Console.WriteLine($"[worker] watcher error: {exception.Message}"),
    BurstDelay = TimeSpan.FromMilliseconds(100),
});

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
Task workerTask = worker.RunAsync(cts.Token);

// ワーカー起動後に bundle を投入する。temp 名に書いてから rename しているので、
// 受信側からは常に「完成した bundle」だけが見える（記事 4.2 節）。
BundleWriter.WriteBundle(
    incomingDir,
    bundleName: "order-002",
    idempotencyKey: "order-002",
    files: new Dictionary<string, byte[]>
    {
        ["payload.csv"] = Encoding.UTF8.GetBytes("item,qty\nbanana,5\n"),
    });
Console.WriteLine("[sender] order-002 を投入（watcher 通知で拾われるはず）");

// 同じ idempotency key の bundle を重複投入する。
// claim は成功するが、処理済みなので副作用は再実行されない（記事 4.5 節）。
BundleWriter.WriteBundle(
    incomingDir,
    bundleName: "order-001-retry",
    idempotencyKey: "order-001",
    files: new Dictionary<string, byte[]>
    {
        ["payload.csv"] = Encoding.UTF8.GetBytes("item,qty\napple,3\n"),
    });
Console.WriteLine("[sender] order-001-retry を投入（key 重複なので処理はスキップされるはず）");

// 3 bundle すべてが archive に移るまでポーリングで待つ
DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);

while (Directory.GetDirectories(archiveDir).Length < 3)
{
    if (DateTime.UtcNow > deadline)
    {
        Console.WriteLine("[demo] timeout!");
        return 1;
    }

    await Task.Delay(50);
}

cts.Cancel();
await workerTask;

Console.WriteLine($"[demo] archive: {string.Join(", ", Directory.GetDirectories(archiveDir).Select(Path.GetFileName).Order())}");
Console.WriteLine($"[demo] 業務処理の実行回数: {processedCount} 回（3 bundle 中、key 重複の 1 件はスキップ）");
Console.WriteLine("[demo] done");

Directory.Delete(root, recursive: true);
return 0;
