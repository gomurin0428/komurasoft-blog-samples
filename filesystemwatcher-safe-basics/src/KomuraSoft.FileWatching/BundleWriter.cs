namespace KomuraSoft.FileWatching;

/// <summary>
/// 送信側で完了条件を明示するための bundle 書き込みです（記事 4.2 節）。
/// temp 名の directory に全内容と manifest を書き、close してから、
/// 同一ファイルシステム上で final 名に rename します。
/// </summary>
public static class BundleWriter
{
    /// <summary>
    /// bundle を incoming ディレクトリに置きます。
    /// 受信側からは、rename された瞬間に「完成した bundle」として見えます。
    /// </summary>
    /// <returns>final 名の bundle directory のパス。</returns>
    public static string WriteBundle(
        string incomingDir,
        string bundleName,
        string idempotencyKey,
        IReadOnlyDictionary<string, byte[]> files)
    {
        string tempDir = Path.Combine(incomingDir, bundleName + BundleScanner.TempSuffix);
        string finalDir = Path.Combine(incomingDir, bundleName);

        // 1. temp 名に全内容を書く
        Directory.CreateDirectory(tempDir);

        foreach ((string fileName, byte[] content) in files)
        {
            File.WriteAllBytes(Path.Combine(tempDir, fileName), content);
        }

        // 2. manifest（完了の明示 + idempotency key）を最後に置く
        new BundleManifest(idempotencyKey).WriteToFile(Path.Combine(tempDir, BundleManifest.FileName));

        // 3. 同一ファイルシステム上で rename する
        Directory.Move(tempDir, finalDir);

        return finalDir;
    }
}
