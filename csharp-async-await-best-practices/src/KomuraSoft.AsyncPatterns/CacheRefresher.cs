namespace KomuraSoft.AsyncPatterns;

/// <summary>
/// await をまたぐ排他に SemaphoreSlim を使う例です（記事 3.11）。
/// WaitAsync で入り、Release は finally で必ず呼びます。
/// 記事では RefreshCoreAsync は Task.Delay(1 秒) 固定ですが、
/// テストから検証できるよう、実処理をコンストラクタで差し替えられるようにしています。
/// </summary>
public sealed class CacheRefresher
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Func<CancellationToken, Task>? _refreshCore;

    public CacheRefresher(Func<CancellationToken, Task>? refreshCore = null)
    {
        _refreshCore = refreshCore;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await RefreshCoreAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private Task RefreshCoreAsync(CancellationToken cancellationToken)
        => _refreshCore is null
            ? Task.Delay(TimeSpan.FromSeconds(1), cancellationToken)
            : _refreshCore(cancellationToken);
}
