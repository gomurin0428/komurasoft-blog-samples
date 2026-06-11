using Xunit;

namespace KomuraSoft.UiThreadAsyncAwait.Tests;

public sealed class DocumentRepositoryTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public void Dispose()
    {
        if (File.Exists(_tempPath))
        {
            File.Delete(_tempPath);
        }
    }

    [Fact]
    public async Task LoadNormalizedTextAsync_CrLfをLfへ正規化する()
    {
        await File.WriteAllTextAsync(_tempPath, "1行目\r\n2行目\r\n3行目");

        DocumentRepository repository = new();
        string text = await repository.LoadNormalizedTextAsync(_tempPath, CancellationToken.None);

        Assert.Equal("1行目\n2行目\n3行目", text);
    }

    [Fact]
    public async Task LoadNormalizedTextAsync_Lfのみの入力はそのまま返す()
    {
        await File.WriteAllTextAsync(_tempPath, "a\nb\nc");

        DocumentRepository repository = new();
        string text = await repository.LoadNormalizedTextAsync(_tempPath, CancellationToken.None);

        Assert.Equal("a\nb\nc", text);
    }

    [Fact]
    public async Task LoadNormalizedTextAsync_キャンセル済みトークンでは例外になる()
    {
        await File.WriteAllTextAsync(_tempPath, "text");

        DocumentRepository repository = new();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.LoadNormalizedTextAsync(_tempPath, cts.Token));
    }
}
