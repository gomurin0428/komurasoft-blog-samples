using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericHostSample;

/// <summary>
/// DI コンテナに登録するアプリケーションサービスの例。
/// コンストラクタインジェクションでオプションとロガーを受け取る。
/// </summary>
public interface IGreetingService
{
    string BuildGreeting();
}

public sealed class GreetingService : IGreetingService
{
    private readonly GreetingOptions _options;
    private readonly ILogger<GreetingService> _logger;

    public GreetingService(IOptions<GreetingOptions> options, ILogger<GreetingService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string BuildGreeting()
    {
        _logger.LogDebug("Building greeting message.");
        return $"{_options.Message} (at {DateTimeOffset.Now:HH:mm:ss})";
    }
}
