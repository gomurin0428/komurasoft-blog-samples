namespace KomuraSoft.AsyncPatterns;

/// <summary>
/// 一定間隔の非同期処理を PeriodicTimer で回す例です（記事 3.8）。
/// 1 つのタイマーに対して、同時に複数の WaitForNextTickAsync を飛ばさない前提で使います。
/// 記事では TimeSpan.FromSeconds(10) 固定ですが、テスト・デモから扱えるよう
/// 周期と更新処理をコンストラクタで受け取る形にしています。
/// </summary>
public sealed class PeriodicCacheRefresher
{
    private readonly TimeSpan _period;
    private readonly Func<CancellationToken, Task> _refreshCache;

    public PeriodicCacheRefresher(TimeSpan period, Func<CancellationToken, Task> refreshCache)
    {
        ArgumentNullException.ThrowIfNull(refreshCache);
        _period = period;
        _refreshCache = refreshCache;
    }

    public async Task RunPeriodicAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_period);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await RefreshCacheAsync(cancellationToken);
        }
    }

    private Task RefreshCacheAsync(CancellationToken cancellationToken)
        => _refreshCache(cancellationToken);
}
