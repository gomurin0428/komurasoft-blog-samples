namespace KomuraSoft.FileIntegration;

/// <summary>
/// temp -> close -> rename / replace でファイルを公開します（記事 4.1 / 5.2 章）。
/// final 名が見えた時点で「内容は完成済み」という約束を、実装で保証します。
/// </summary>
public static class AtomicFilePublisher
{
    /// <summary>
    /// final と同じディレクトリに一意な temp パスを作ります。
    /// temp と final を同一ディレクトリ（同一ボリューム / ファイルシステム）に置くことで、
    /// rename がコピーに化けたり Replace が失敗したりするのを防ぎます。
    /// </summary>
    public static string MakeTempPathSameDirectory(string finalPath)
    {
        string fullPath = Path.GetFullPath(finalPath);
        string directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException($"Cannot resolve directory of '{finalPath}'.", nameof(finalPath));
        string fileName = Path.GetFileName(fullPath);
        return Path.Combine(directory, $"{fileName}.{Guid.NewGuid():N}.tmp");
    }

    /// <summary>
    /// temp に全内容を書き込み、flush / close してから final 名へ rename / replace します。
    /// 書き込み中は final 名が一切見えないため、受信側が途中のデータを読むことはありません。
    /// temp は FileShare.None で開くので、生成中のファイルを他プロセスが開くこともできません。
    /// </summary>
    public static void Publish(string finalPath, Action<Stream> writePayload)
    {
        string tempPath = MakeTempPathSameDirectory(finalPath);
        try
        {
            using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,   // 「無ければ作る」を 1 操作で（記事 3.1 章）
                FileAccess.Write,
                FileShare.None))      // 生成中は誰にも触らせない
            {
                writePayload(stream);
                stream.Flush(flushToDisk: true);
            } // ここで close

            PublishByRenameOrReplace(tempPath, finalPath);
        }
        catch
        {
            TryDeleteQuietly(tempPath);
            throw;
        }
    }

    /// <summary>UTF-8 テキストを原子的に公開します。</summary>
    public static void PublishText(string finalPath, string content)
    {
        Publish(finalPath, stream =>
        {
            using var writer = new StreamWriter(stream, leaveOpen: true);
            writer.Write(content);
        });
    }

    /// <summary>
    /// 同一ディレクトリ（同一ボリューム）前提の rename / replace。
    /// final が既に存在する場合は置き換えます。
    /// </summary>
    public static void PublishByRenameOrReplace(string tempPath, string finalPath)
    {
        File.Move(tempPath, finalPath, overwrite: true);
    }

    /// <summary>
    /// done / manifest ファイルを公開します（記事 4.2 / 5.2 章）。
    /// 必ず本体（payload）を公開した「あと」に呼びます。先に done を置くと事故予告になります。
    /// </summary>
    public static void PublishDoneFile(string donePath, TransferManifest manifest)
    {
        Publish(donePath, manifest.WriteTo);
    }

    private static void TryDeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // 後始末に失敗しても元の例外を優先する
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
