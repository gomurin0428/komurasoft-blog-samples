using System.Collections.Concurrent;
using Xunit;

namespace KomuraSoft.FileWatching.Tests;

/// <summary>
/// 一時ディレクトリ上で実際にファイルを作成・変更し、
/// FileSystemWatcher の通知から archive までの流れを検証する統合テストです。
/// イベントのタイミングには依存せず、結果（archive への移動）をポーリングで待ちます。
/// </summary>
public class BundleProcessingWorkerTests
{
    private static BundleProcessingWorkerOptions CreateOptions(
        TestDirectory dir,
        string workerName,
        Action<string> processBundle) => new()
        {
            IncomingDirectory = dir.IncomingDir,
            ProcessingDirectory = dir.ProcessingDir(workerName),
            ArchiveDirectory = dir.ArchiveDir,
            ProcessedStore = new FileProcessedStore(dir.ProcessedDir),
            ProcessBundle = processBundle,
            BurstDelay = TimeSpan.FromMilliseconds(50),
        };

    private static void WriteBundle(TestDirectory dir, string bundleName, string idempotencyKey)
        => BundleWriter.WriteBundle(
            dir.IncomingDir,
            bundleName,
            idempotencyKey,
            files: new Dictionary<string, byte[]>
            {
                ["payload.csv"] = System.Text.Encoding.UTF8.GetBytes($"item,qty\n{bundleName},1\n"),
            });

    private static bool IsArchived(TestDirectory dir, string bundleName)
        => Directory.Exists(dir.ArchiveDir)
            && Directory.EnumerateDirectories(dir.ArchiveDir)
                .Select(Path.GetFileName)
                .Any(name => name == bundleName || (name?.StartsWith(bundleName + ".") ?? false));

    [Fact]
    public async Task StartupFullRescan_ProcessesBundlePlacedBeforeStart()
    {
        using var dir = new TestDirectory();
        Directory.CreateDirectory(dir.IncomingDir);

        // ワーカー起動前から置かれていた bundle は、イベントでは拾えない（記事 4.4 節）
        WriteBundle(dir, "order-001", "order-001");

        var processed = new ConcurrentQueue<string>();
        using var cts = new CancellationTokenSource();
        using var worker = new BundleProcessingWorker(
            CreateOptions(dir, "worker1", path => processed.Enqueue(Path.GetFileName(path))));

        Task workerTask = worker.RunAsync(cts.Token);

        await Wait.UntilAsync(() => IsArchived(dir, "order-001"), "order-001 is archived");

        cts.Cancel();
        await workerTask;

        Assert.Equal(["order-001"], processed);
        Assert.Empty(Directory.EnumerateDirectories(dir.IncomingDir));
    }

    [Fact]
    public async Task WatcherNotification_ProcessesBundleDroppedAfterStart()
    {
        using var dir = new TestDirectory();

        var processed = new ConcurrentQueue<string>();
        using var cts = new CancellationTokenSource();
        using var worker = new BundleProcessingWorker(
            CreateOptions(dir, "worker1", path => processed.Enqueue(Path.GetFileName(path))));

        Task workerTask = worker.RunAsync(cts.Token);

        // 起動後に bundle を投入する（temp 名に書いて rename → Renamed 通知が出る）
        WriteBundle(dir, "order-002", "order-002");
        WriteBundle(dir, "order-003", "order-003");

        await Wait.UntilAsync(
            () => IsArchived(dir, "order-002") && IsArchived(dir, "order-003"),
            "order-002 and order-003 are archived");

        cts.Cancel();
        await workerTask;

        Assert.Equal(["order-002", "order-003"], processed.Order());
    }

    [Fact]
    public async Task DuplicateIdempotencyKey_IsArchivedWithoutReprocessing()
    {
        using var dir = new TestDirectory();

        var processed = new ConcurrentQueue<string>();
        using var cts = new CancellationTokenSource();
        using var worker = new BundleProcessingWorker(
            CreateOptions(dir, "worker1", path => processed.Enqueue(Path.GetFileName(path))));

        Task workerTask = worker.RunAsync(cts.Token);

        WriteBundle(dir, "order-004", "order-004");
        await Wait.UntilAsync(() => IsArchived(dir, "order-004"), "order-004 is archived");

        // 同じ idempotency key で再投入する（重複通知や再送の再現）
        WriteBundle(dir, "order-004-retry", "order-004");
        await Wait.UntilAsync(() => IsArchived(dir, "order-004-retry"), "order-004-retry is archived");

        cts.Cancel();
        await workerTask;

        // 重複分は archive には移るが、業務処理は再実行されない（記事 4.5 節）
        Assert.Equal(["order-004"], processed);
    }

    [Fact]
    public async Task TempBundle_IsNotProcessedUntilRenamed()
    {
        using var dir = new TestDirectory();

        var processed = new ConcurrentQueue<string>();
        using var cts = new CancellationTokenSource();
        using var worker = new BundleProcessingWorker(
            CreateOptions(dir, "worker1", path => processed.Enqueue(Path.GetFileName(path))));

        Task workerTask = worker.RunAsync(cts.Token);

        // 書き込み途中（temp 名のまま）の bundle を置く
        string tempBundle = Path.Combine(dir.IncomingDir, "order-005" + BundleScanner.TempSuffix);
        Directory.CreateDirectory(tempBundle);
        File.WriteAllText(Path.Combine(tempBundle, "payload.csv"), "item,qty\norder-005,1\n");
        new BundleManifest("order-005").WriteToFile(Path.Combine(tempBundle, BundleManifest.FileName));

        // 完成済みの別 bundle が処理される間も、temp 名の bundle は触られない
        WriteBundle(dir, "order-006", "order-006");
        await Wait.UntilAsync(() => IsArchived(dir, "order-006"), "order-006 is archived");

        Assert.True(Directory.Exists(tempBundle));
        Assert.DoesNotContain("order-005" + BundleScanner.TempSuffix, processed);

        // rename で完了を明示した瞬間から、処理対象になる（記事 4.2 節）
        Directory.Move(tempBundle, Path.Combine(dir.IncomingDir, "order-005"));
        await Wait.UntilAsync(() => IsArchived(dir, "order-005"), "order-005 is archived");

        cts.Cancel();
        await workerTask;

        Assert.Equal(["order-005", "order-006"], processed.Order());
    }

    [Fact]
    public async Task TwoWorkers_EachBundleIsProcessedExactlyOnce()
    {
        using var dir = new TestDirectory();

        var processed = new ConcurrentQueue<string>();
        using var cts = new CancellationTokenSource();

        // incoming / archive / processed store を共有する 2 ワーカー（記事 4.3 節）
        using var worker1 = new BundleProcessingWorker(
            CreateOptions(dir, "worker1", path => processed.Enqueue(Path.GetFileName(path))));
        using var worker2 = new BundleProcessingWorker(
            CreateOptions(dir, "worker2", path => processed.Enqueue(Path.GetFileName(path))));

        Task task1 = worker1.RunAsync(cts.Token);
        Task task2 = worker2.RunAsync(cts.Token);

        string[] bundleNames = Enumerable.Range(0, 10)
            .Select(i => $"order-{100 + i}")
            .ToArray();

        foreach (string bundleName in bundleNames)
        {
            WriteBundle(dir, bundleName, bundleName);
        }

        await Wait.UntilAsync(
            () => bundleNames.All(name => IsArchived(dir, name)),
            "all bundles are archived");

        cts.Cancel();
        await Task.WhenAll(task1, task2);

        // claim rename により、どの bundle もちょうど 1 回だけ処理される
        Assert.Equal(bundleNames.Order(), processed.Order());
    }
}
