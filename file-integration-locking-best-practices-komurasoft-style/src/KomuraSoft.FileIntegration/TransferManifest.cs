using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KomuraSoft.FileIntegration;

/// <summary>
/// done / manifest ファイルの内容（記事 4.2 章）。
/// 「何が完成したか」をデータ本体とは別ファイルで明示します。
/// </summary>
public sealed record TransferManifest(
    string FileName,
    long Size,
    string Hash,
    string IdempotencyKey,
    DateTimeOffset CreatedAt)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>公開済みの payload からマニフェストを作ります。</summary>
    public static TransferManifest CreateFor(string payloadPath, string idempotencyKey)
    {
        var info = new FileInfo(payloadPath);
        return new TransferManifest(
            FileName: info.Name,
            Size: info.Length,
            Hash: ComputeHash(payloadPath),
            IdempotencyKey: idempotencyKey,
            CreatedAt: DateTimeOffset.UtcNow);
    }

    /// <summary>done / manifest ファイルを読みます（記事 5.2 章の ReadDoneFile）。</summary>
    public static TransferManifest ReadDoneFile(string donePath)
    {
        using FileStream stream = OpenForSharedRead(donePath);
        return JsonSerializer.Deserialize<TransferManifest>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Manifest '{donePath}' is empty.");
    }

    /// <summary>payload がマニフェストの内容と一致するか検証します（記事 5.2 章の VerifyPayload）。</summary>
    public void VerifyPayload(string payloadPath)
    {
        var info = new FileInfo(payloadPath);
        if (!info.Exists)
        {
            throw new InvalidDataException($"Payload '{payloadPath}' does not exist.");
        }

        if (info.Length != Size)
        {
            throw new InvalidDataException(
                $"Payload size mismatch: expected {Size} bytes but found {info.Length} bytes.");
        }

        string actualHash = ComputeHash(payloadPath);
        if (!string.Equals(actualHash, Hash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Payload hash mismatch: expected {Hash} but found {actualHash}.");
        }
    }

    /// <summary>SHA-256 ハッシュ（16進小文字）を計算します。</summary>
    public static string ComputeHash(string path)
    {
        using FileStream stream = OpenForSharedRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    internal void WriteTo(Stream stream)
        => JsonSerializer.Serialize(stream, this, JsonOptions);

    /// <summary>
    /// 読み取りは FileShare.Read で開きます。
    /// 他の読み手と共存しつつ、書き込み中のファイルを開いてしまった場合は IOException で気付けます。
    /// </summary>
    private static FileStream OpenForSharedRead(string path)
        => new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
}
