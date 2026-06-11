namespace KomuraSoft.UiThreadAsyncAwait;

/// <summary>
/// UI や特定アプリモデルに依存しない汎用ライブラリコードの例（記事 4.3 章）。
/// UI を触らないので ConfigureAwait(false) が自然に書ける。
/// WPF でも WinForms でも ASP.NET Core でも worker でも使える形。
/// </summary>
public sealed class DocumentRepository
{
    public async Task<string> LoadNormalizedTextAsync(string path, CancellationToken cancellationToken)
    {
        string text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return text.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
