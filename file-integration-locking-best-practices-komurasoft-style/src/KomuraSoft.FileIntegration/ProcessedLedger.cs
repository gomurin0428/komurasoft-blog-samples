namespace KomuraSoft.FileIntegration;

/// <summary>
/// 処理済み台帳（記事 4.5 / 5.2 章）。
/// idempotency key ごとにマーカーファイルを 1 つ置くだけの単純な実装です。
/// マーカーの作成は FileMode.CreateNew による原子的作成なので、
/// 排他が一度破れて同じ入力が二重に来ても、記録に成功するのは 1 回だけです。
/// </summary>
public sealed class ProcessedLedger
{
    private readonly string _ledgerDir;

    public ProcessedLedger(string ledgerDir)
    {
        _ledgerDir = ledgerDir;
        Directory.CreateDirectory(ledgerDir);
    }

    /// <summary>この idempotency key が処理済みかどうかを返します。</summary>
    public bool AlreadyProcessed(string idempotencyKey)
        => File.Exists(MarkerPathFor(idempotencyKey));

    /// <summary>
    /// 処理済みとして記録します。既に記録されていた場合は false を返します
    /// （その場合、別のワーカーが先に処理を終えています）。
    /// </summary>
    public bool TryRecordProcessed(string idempotencyKey)
    {
        try
        {
            using var stream = new FileStream(
                MarkerPathFor(idempotencyKey),
                FileMode.CreateNew,   // 「無ければ作る」を 1 操作で
                FileAccess.Write,
                FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write(DateTimeOffset.UtcNow.ToString("O"));
            return true;
        }
        catch (IOException)
        {
            return false; // 既に記録済み
        }
    }

    /// <summary>処理済みとして記録します（記事 5.2 章の RecordProcessed）。</summary>
    public void RecordProcessed(string idempotencyKey)
        => TryRecordProcessed(idempotencyKey);

    private string MarkerPathFor(string idempotencyKey)
    {
        // idempotency key をそのままファイル名にできるよう、使えない文字を逃がす
        char[] invalid = Path.GetInvalidFileNameChars();
        string safe = string.Concat(idempotencyKey.Select(c => invalid.Contains(c) ? '_' : c));
        return Path.Combine(_ledgerDir, safe + ".processed");
    }
}
