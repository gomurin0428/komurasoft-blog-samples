namespace KomuraSoft.FileWatching;

/// <summary>
/// FileSystemWatcher の通知を「再スキャン要求」に畳み、
/// 再スキャン → 原子的 claim → idempotency 確認 → 処理 → archive の流れで
/// bundle を処理するワーカーです（記事 4 章・5.2 節）。
/// </summary>
public sealed class BundleProcessingWorker : IDisposable
{
    private readonly SemaphoreSlim _scanSignal = new(0, int.MaxValue);
    private int _scanRequested = 0;
    private int _fullRescanRequested = 0;

    private readonly string incomingDir;
    private readonly string processingDir;
    private readonly string archiveDir;

    private readonly IProcessedStore _processedStore;
    private readonly Action<string> _processBundle;
    private readonly Action<Exception>? _log;
    private readonly TimeSpan _burstDelay;
    private readonly TimeSpan _fullRescanInterval;
    private readonly FileSystemWatcher _watcher;

    public BundleProcessingWorker(BundleProcessingWorkerOptions options)
    {
        incomingDir = options.IncomingDirectory;
        processingDir = options.ProcessingDirectory;
        archiveDir = options.ArchiveDirectory;
        _processedStore = options.ProcessedStore;
        _processBundle = options.ProcessBundle;
        _log = options.Log;
        _burstDelay = options.BurstDelay;
        _fullRescanInterval = options.FullRescanInterval;

        Directory.CreateDirectory(incomingDir);
        Directory.CreateDirectory(processingDir);
        Directory.CreateDirectory(archiveDir);

        // 監視対象は incoming 直下の bundle directory の出現だけでよいので、
        // IncludeSubdirectories は false、NotifyFilter も必要最小限にする（記事 3.4 節）
        _watcher = new FileSystemWatcher(incomingDir)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName,
        };

        // イベントハンドラは「再スキャン要求を立ててすぐ返す」だけにする（記事 4.1 節）
        _watcher.Created += OnAnyChange;
        _watcher.Changed += OnAnyChange;
        _watcher.Deleted += OnAnyChange;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// 走査 worker を起動します。startup full rescan と、
    /// 保険としての定期 full rescan（記事 4.4 節）も合わせて動かします。
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Task scanner = ScannerLoopAsync(cancellationToken);
        Task periodic = PeriodicFullRescanLoopAsync(cancellationToken);

        try
        {
            await Task.WhenAll(scanner, periodic);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 停止要求による終了は正常
        }
    }

    void OnAnyChange(object? sender, FileSystemEventArgs e)
    {
        RequestScan(full: false);
    }

    void OnRenamed(object? sender, RenamedEventArgs e)
    {
        RequestScan(full: false);
    }

    void OnError(object? sender, ErrorEventArgs e)
    {
        Log(e.GetException());
        RequestScan(full: true);
    }

    void RequestScan(bool full)
    {
        if (full)
        {
            Interlocked.Exchange(ref _fullRescanRequested, 1);
        }

        if (Interlocked.Exchange(ref _scanRequested, 1) == 0)
        {
            _scanSignal.Release();
        }
    }

    async Task ScannerLoopAsync(CancellationToken cancellationToken)
    {
        RequestScan(full: true); // startup scan

        while (!cancellationToken.IsCancellationRequested)
        {
            await _scanSignal.WaitAsync(cancellationToken);

            // 通知バーストを少しまとめる
            await Task.Delay(_burstDelay, cancellationToken);

            Interlocked.Exchange(ref _scanRequested, 0);
            bool full = Interlocked.Exchange(ref _fullRescanRequested, 0) == 1;

            foreach (var bundle in EnumerateReadyBundles(incomingDir, full))
            {
                var claimedPath = Path.Combine(processingDir, bundle.Name);

                if (!TryClaimByRename(bundle.Path, claimedPath))
                {
                    continue; // 他ワーカーが先に取得
                }

                var manifest = ReadManifest(Path.Combine(claimedPath, "manifest.json"));

                if (AlreadyProcessed(manifest.IdempotencyKey))
                {
                    MoveToArchive(claimedPath, archiveDir);
                    continue;
                }

                ProcessBundle(claimedPath);
                RecordProcessed(manifest.IdempotencyKey);
                MoveToArchive(claimedPath, archiveDir);
            }

            if (Volatile.Read(ref _scanRequested) == 1)
            {
                _scanSignal.Release(); // 走査中に来た通知を取りこぼさない
            }
        }
    }

    async Task PeriodicFullRescanLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_fullRescanInterval, cancellationToken);

            // 定期的な保険としての full rescan（記事 4.4 節）
            RequestScan(full: true);
        }
    }

    private static IEnumerable<ReadyBundle> EnumerateReadyBundles(string incomingDir, bool full)
        => BundleScanner.EnumerateReadyBundles(incomingDir, full);

    private static bool TryClaimByRename(string bundlePath, string claimedPath)
        => AtomicClaim.TryClaimByRename(bundlePath, claimedPath);

    private static BundleManifest ReadManifest(string manifestPath)
        => BundleManifest.ReadFromFile(manifestPath);

    private bool AlreadyProcessed(string idempotencyKey)
        => _processedStore.AlreadyProcessed(idempotencyKey);

    private void RecordProcessed(string idempotencyKey)
        => _processedStore.RecordProcessed(idempotencyKey);

    private void ProcessBundle(string claimedPath)
        => _processBundle(claimedPath);

    private static void MoveToArchive(string claimedPath, string archiveDir)
    {
        string destination = Path.Combine(archiveDir, Path.GetFileName(claimedPath));

        if (Directory.Exists(destination))
        {
            // 同名 bundle が既に archive にある場合（idempotency key 重複など）は退避名を付ける
            destination = $"{destination}.{Guid.NewGuid():N}";
        }

        Directory.Move(claimedPath, destination);
    }

    private void Log(Exception exception)
        => _log?.Invoke(exception);

    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _scanSignal.Dispose();
    }
}
