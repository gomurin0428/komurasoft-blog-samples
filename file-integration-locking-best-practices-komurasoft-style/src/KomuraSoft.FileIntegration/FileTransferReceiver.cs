namespace KomuraSoft.FileIntegration;

/// <summary>1 バンドルの処理結果。</summary>
public enum ReceiveResult
{
    /// <summary>このワーカーが処理して archive へ移した。</summary>
    Processed,

    /// <summary>他のワーカーが先に claim を取った（正常。何もしない）。</summary>
    ClaimedByOther,

    /// <summary>idempotency key が処理済みだったため、二重実行せず archive へ移した。</summary>
    SkippedAsDuplicate,

    /// <summary>manifest との照合に失敗したため error へ移した。</summary>
    MovedToError,
}

/// <summary>
/// 受信側の手順（記事 5.2 章の「正しい方向の例」）。
/// claim を取る -> manifest を検証する -> 処理済みなら skip -> 処理する -> 記録する -> archive へ、
/// という順序を 1 か所に閉じ込めます。
/// </summary>
public sealed class FileTransferReceiver
{
    private readonly string _incomingDir;
    private readonly string _processingDir;
    private readonly string _archiveDir;
    private readonly string _errorDir;
    private readonly ProcessedLedger _ledger;
    private readonly Action<string> _process;

    public FileTransferReceiver(
        string incomingDir,
        string processingDir,
        string archiveDir,
        string errorDir,
        ProcessedLedger ledger,
        Action<string> process)
    {
        _incomingDir = incomingDir;
        _processingDir = processingDir;
        _archiveDir = archiveDir;
        _errorDir = errorDir;
        _ledger = ledger;
        _process = process;
    }

    /// <summary>incoming で「読んでよい」状態になったバンドルを列挙します。</summary>
    public IReadOnlyList<string> ListReadyBaseNames()
        => FileClaimer.ListReadyBaseNames(_incomingDir);

    /// <summary>
    /// 1 バンドルを処理します。記事 5.2 章の擬似コードと同じ流れです。
    /// </summary>
    public ReceiveResult ProcessBundle(string baseName)
    {
        if (!FileClaimer.TryClaimBundleByRename(baseName, _incomingDir, _processingDir))
        {
            return ReceiveResult.ClaimedByOther; // 他ワーカーが先に取得
        }

        TransferManifest manifest;
        try
        {
            manifest = TransferManifest.ReadDoneFile(
                Path.Combine(_processingDir, baseName + ".done"));
            manifest.VerifyPayload(Path.Combine(_processingDir, baseName));
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            FileClaimer.MoveBundle(_processingDir, _errorDir, baseName);
            return ReceiveResult.MovedToError;
        }

        if (_ledger.AlreadyProcessed(manifest.IdempotencyKey))
        {
            FileClaimer.MoveBundle(_processingDir, _archiveDir, baseName);
            return ReceiveResult.SkippedAsDuplicate;
        }

        _process(Path.Combine(_processingDir, baseName));
        _ledger.RecordProcessed(manifest.IdempotencyKey);
        FileClaimer.MoveBundle(_processingDir, _archiveDir, baseName);
        return ReceiveResult.Processed;
    }
}
