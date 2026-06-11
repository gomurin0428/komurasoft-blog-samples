using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KomuraSoft.TimerSelection;

/// <summary>
/// async な定期処理を <see cref="PeriodicTimer"/> で回す例です（記事 4.1）。
/// 「待つ → 処理する → また待つ」を 1 本の async メソッドの流れとして書けて、
/// <see cref="CancellationToken"/> もそのまま下流へ渡せます。
/// </summary>
public sealed class CacheRefreshWorker : BackgroundService
{
    private readonly ILogger<CacheRefreshWorker> _logger;

    public CacheRefreshWorker(ILogger<CacheRefreshWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CacheRefreshWorker started.");

        await RefreshCacheAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RefreshCacheAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CacheRefreshWorker stopping.");
        }
    }

    private async Task RefreshCacheAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing cache...");
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
    }
}
