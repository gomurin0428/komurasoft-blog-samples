using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericHostSample;

/// <summary>
/// BackgroundService を継承した常駐ワーカーの例。
/// ホストの起動とともに動き出し、Ctrl+C や SIGTERM で
/// CancellationToken 経由のグレースフルシャットダウンを受け取る。
/// </summary>
public sealed class Worker : BackgroundService
{
    private readonly IGreetingService _greetingService;
    private readonly GreetingOptions _options;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IGreetingService greetingService,
        IOptions<GreetingOptions> options,
        ILogger<Worker> logger)
    {
        _greetingService = greetingService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started. Interval: {Interval}s", _options.IntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogInformation("{Greeting}", _greetingService.BuildGreeting());
            }
        }
        catch (OperationCanceledException)
        {
            // Ctrl+C / SIGTERM による停止要求。後始末をしてから抜ける。
            _logger.LogInformation("Stop requested. Cleaning up...");
        }

        _logger.LogInformation("Worker stopped gracefully.");
    }
}
