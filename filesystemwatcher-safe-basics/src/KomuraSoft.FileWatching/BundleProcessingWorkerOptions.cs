namespace KomuraSoft.FileWatching;

/// <summary>
/// <see cref="BundleProcessingWorker"/> の設定です。
/// </summary>
public sealed class BundleProcessingWorkerOptions
{
    /// <summary>送信側が bundle を置く監視対象ディレクトリ。</summary>
    public required string IncomingDirectory { get; init; }

    /// <summary>このワーカー専用の processing ディレクトリ（claim rename の移動先）。</summary>
    public required string ProcessingDirectory { get; init; }

    /// <summary>処理済み bundle の移動先。</summary>
    public required string ArchiveDirectory { get; init; }

    /// <summary>idempotency key の照合と記録に使うストア（記事 4.5 節）。</summary>
    public required IProcessedStore ProcessedStore { get; init; }

    /// <summary>claim 済み bundle directory のパスを受け取る業務処理。</summary>
    public required Action<string> ProcessBundle { get; init; }

    /// <summary>watcher の Error など、回復対象の例外を受け取るログ出力。</summary>
    public Action<Exception>? Log { get; init; }

    /// <summary>通知バーストをまとめる待ち時間（記事 4.1 節。100〜300ms 程度が目安）。</summary>
    public TimeSpan BurstDelay { get; init; } = TimeSpan.FromMilliseconds(200);

    /// <summary>保険として定期的に full rescan する間隔（記事 4.4 節）。</summary>
    public TimeSpan FullRescanInterval { get; init; } = TimeSpan.FromSeconds(30);
}
