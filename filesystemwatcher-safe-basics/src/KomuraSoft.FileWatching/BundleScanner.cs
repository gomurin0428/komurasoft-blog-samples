namespace KomuraSoft.FileWatching;

/// <summary>
/// incoming ディレクトリで見つかった、処理してよい bundle directory です。
/// </summary>
public sealed record ReadyBundle(string Name, string Path);

/// <summary>
/// incoming ディレクトリを再スキャンして ready な bundle を列挙します（記事 4.1 節・4.4 節）。
/// イベント列から状態を復元するのではなく、毎回ディスク上の現物を見ます。
/// </summary>
public static class BundleScanner
{
    /// <summary>書き込み途中の bundle に付ける一時サフィックス。</summary>
    public const string TempSuffix = ".tmp";

    /// <summary>
    /// 処理してよい bundle directory を列挙します。
    /// 「真実はディレクトリ再スキャン」なので、incremental（full: false）でも
    /// full rescan でもやることは同じで、いまディスク上に見えている現物を走査します。
    /// </summary>
    public static IEnumerable<ReadyBundle> EnumerateReadyBundles(string incomingDir, bool full)
    {
        if (!Directory.Exists(incomingDir))
        {
            yield break;
        }

        foreach (string bundlePath in Directory.EnumerateDirectories(incomingDir))
        {
            string name = Path.GetFileName(bundlePath);

            // temp 名のままの bundle は書き込み途中なので触らない（記事 4.2 節）
            if (name.EndsWith(TempSuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // manifest.json が置かれていることを「完了の明示」とみなす（記事 4.2 節）
            if (!File.Exists(Path.Combine(bundlePath, BundleManifest.FileName)))
            {
                continue;
            }

            yield return new ReadyBundle(name, bundlePath);
        }
    }
}
