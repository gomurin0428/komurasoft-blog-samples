using Microsoft.Extensions.Logging;

namespace KomuraSoft.TimerSelection.Tests;

/// <summary>
/// ログメッセージをメモリに溜めるだけのテスト用ロガーです。
/// タイマーの callback など別スレッドからも書かれるため、ロックで保護します。
/// </summary>
public sealed class ListLogger<T> : ILogger<T>
{
    private readonly object _gate = new();
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Snapshot()
    {
        lock (_gate)
        {
            return _messages.ToArray();
        }
    }

    public bool Contains(string fragment)
        => Snapshot().Any(m => m.Contains(fragment, StringComparison.Ordinal));

    public int Count(string fragment)
        => Snapshot().Count(m => m.Contains(fragment, StringComparison.Ordinal));

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        lock (_gate)
        {
            _messages.Add(formatter(state, exception));
        }
    }
}
