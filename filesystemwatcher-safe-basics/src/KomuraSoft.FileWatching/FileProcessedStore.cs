using System.Security.Cryptography;
using System.Text;

namespace KomuraSoft.FileWatching;

/// <summary>
/// idempotency key の照合と記録を行うストアです（記事 4.5 節）。
/// </summary>
public interface IProcessedStore
{
    /// <summary>この key の bundle が処理済みかどうかを返します。</summary>
    bool AlreadyProcessed(string idempotencyKey);

    /// <summary>この key の bundle を処理済みとして記録します。</summary>
    void RecordProcessed(string idempotencyKey);
}

/// <summary>
/// マーカーファイルで処理済み key を記録する、ファイルベースの実装です。
/// 複数ワーカーから同じディレクトリを共有できます。
/// </summary>
public sealed class FileProcessedStore : IProcessedStore
{
    private readonly string _storeDir;

    public FileProcessedStore(string storeDir)
    {
        _storeDir = storeDir;
        Directory.CreateDirectory(storeDir);
    }

    public bool AlreadyProcessed(string idempotencyKey)
        => File.Exists(MarkerPath(idempotencyKey));

    public void RecordProcessed(string idempotencyKey)
    {
        string marker = MarkerPath(idempotencyKey);
        string temp = $"{marker}.{Guid.NewGuid():N}{BundleScanner.TempSuffix}";

        // temp に書いてから rename する（記事 4.2 節と同じ理屈）
        File.WriteAllText(temp, idempotencyKey);

        try
        {
            File.Move(temp, marker);
        }
        catch (IOException)
        {
            // 他ワーカーが先に記録済みなら、それでよい（at-least-once 前提）
            File.Delete(temp);
        }
    }

    private string MarkerPath(string idempotencyKey)
    {
        // key をそのままファイル名にすると使えない文字で困るので、ハッシュ名にする
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey));
        return Path.Combine(_storeDir, $"{Convert.ToHexString(hash)}.done");
    }
}
