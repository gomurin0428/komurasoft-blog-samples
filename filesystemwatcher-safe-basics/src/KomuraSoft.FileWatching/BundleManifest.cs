using System.Text.Json;

namespace KomuraSoft.FileWatching;

/// <summary>
/// bundle に同梱する manifest です。
/// IdempotencyKey を入れておくと、重複通知や full rescan で同じ bundle を
/// 複数回見ても、副作用を再実行せずに済みます（記事 4.5 節）。
/// </summary>
public sealed record BundleManifest(string IdempotencyKey)
{
    /// <summary>bundle directory 内での manifest のファイル名。</summary>
    public const string FileName = "manifest.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    /// <summary>manifest.json を読み取ります。</summary>
    public static BundleManifest ReadFromFile(string manifestPath)
    {
        using FileStream stream = File.OpenRead(manifestPath);

        return JsonSerializer.Deserialize<BundleManifest>(stream, SerializerOptions)
            ?? throw new InvalidDataException($"Manifest is empty: {manifestPath}");
    }

    /// <summary>manifest.json として書き出します。</summary>
    public void WriteToFile(string manifestPath)
    {
        using FileStream stream = File.Create(manifestPath);
        JsonSerializer.Serialize(stream, this, SerializerOptions);
    }
}
