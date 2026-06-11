namespace KomuraSoft.FileIntegration;

/// <summary>
/// 送信側の手順（記事 5.2 章の「正しい方向の例」）。
/// temp に書く -> close -> rename / replace で公開 -> done file を公開、の順序を守ります。
/// </summary>
public static class FileTransferSender
{
    /// <summary>
    /// payload を incoming へ原子的に公開し、最後に done file を置きます。
    /// 受信側は done file が見えた時点で「読んでよい」と判断できます。
    /// </summary>
    public static TransferManifest PublishBundle(
        string incomingDir,
        string baseName,
        string integrationId,
        Action<Stream> writePayload)
    {
        Directory.CreateDirectory(incomingDir);
        string finalPath = Path.Combine(incomingDir, baseName);

        // 1. temp に全内容を書き、flush / close してから final 名へ rename / replace
        AtomicFilePublisher.Publish(finalPath, writePayload);

        // 2. 本体の公開より「あと」に done file を置く（順序が大事。記事 4.2 章）
        var manifest = TransferManifest.CreateFor(finalPath, integrationId);
        AtomicFilePublisher.PublishDoneFile(finalPath + ".done", manifest);

        return manifest;
    }
}
