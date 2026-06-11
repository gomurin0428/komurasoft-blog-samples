namespace KomuraSoft.FileWatching;

/// <summary>
/// 記事 5.1 節「典型的な失敗パターン」をそのまま収録した比較用のコードです。
/// 実運用に使うためのものではありません。問題点は 4 つあります。
/// <list type="bullet">
/// <item>Created / Changed をそのまま業務処理に結びつけている</item>
/// <item>完了判定が無い</item>
/// <item>overflow 時に full rescan しない</item>
/// <item>同じファイルを何回処理しても止める仕組みが無い</item>
/// </list>
/// </summary>
public static class TypicalFailurePattern
{
    /// <summary>記事 5.1 節のコードを起動します（比較用）。</summary>
    public static FileSystemWatcher Start(string incomingDir, Action<string> processFile)
    {
        var watcher = new FileSystemWatcher(incomingDir)
        {
            Filter = "*.csv",
            IncludeSubdirectories = false,
            EnableRaisingEvents = true,
            InternalBufferSize = 64 * 1024
        };

        watcher.Created += (_, e) =>
        {
            // Created = 完了通知、と思い込んでいる
            processFile(e.FullPath);
        };

        watcher.Changed += (_, e) =>
        {
            // 何度も来るので、とりあえずもう一回処理
            processFile(e.FullPath);
        };

        watcher.Error += (_, e) =>
        {
            Console.WriteLine(e.GetException());
            // 回復しない
        };

        return watcher;
    }
}
