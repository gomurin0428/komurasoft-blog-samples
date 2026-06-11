namespace KomuraSoft.FileWatching.Tests;

/// <summary>
/// タイミング依存で不安定にならないよう、ポーリング + タイムアウトで条件成立を待つヘルパーです。
/// </summary>
public static class Wait
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    public static async Task UntilAsync(Func<bool> condition, string description)
    {
        DateTime deadline = DateTime.UtcNow + DefaultTimeout;

        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Condition was not met within {DefaultTimeout}: {description}");
            }

            await Task.Delay(25);
        }
    }
}
