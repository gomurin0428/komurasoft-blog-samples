using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KomuraSoft.TimerSelection;

/// <summary>
/// 軽い callback を ThreadPool で回す <see cref="Timer"/>（System.Threading.Timer）の例です（記事 4.2）。
/// callback は前回の完了を待たずに重なりうるので、<see cref="Interlocked.Exchange(ref int, int)"/> で
/// 重複起動をガードしています。また、フィールドで参照を保持して GC を防ぎます。
/// </summary>
public sealed class HeartbeatService : IHostedService, IDisposable
{
    private readonly ILogger<HeartbeatService> _logger;
    private Timer? _timer;
    private int _running;

    public HeartbeatService(ILogger<HeartbeatService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(OnTimer, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }

    private void OnTimer(object? state)
    {
        if (Interlocked.Exchange(ref _running, 1) != 0)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Heartbeat: {Now}", DateTimeOffset.Now);
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
