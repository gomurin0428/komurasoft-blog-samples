namespace KomuraSoft.FileIntegration;

/// <summary>
/// 共有フォルダ連携で起きる一時的な IOException（他プロセスが開いている、
/// ウイルススキャナが触っている、など）に対する単純なリトライです。
/// リトライはあくまで一時的な競合への備えであり、排他制御の代わりにはなりません。
/// </summary>
public static class FileRetry
{
    public static T Execute<T>(
        Func<T> action,
        int maxAttempts = 5,
        TimeSpan? delay = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        TimeSpan wait = delay ?? TimeSpan.FromMilliseconds(100);

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return action();
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(wait);
            }
        }
    }

    public static void Execute(
        Action action,
        int maxAttempts = 5,
        TimeSpan? delay = null)
    {
        Execute<object?>(
            () =>
            {
                action();
                return null;
            },
            maxAttempts,
            delay);
    }
}
