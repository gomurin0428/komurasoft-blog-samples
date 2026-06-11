using System.Diagnostics;

namespace KomuraSoft.TimerSelection.Tests;

internal static class TestHelpers
{
    /// <summary>
    /// 条件が成立するまでポーリングして待ちます。
    /// タイマーのテストはタイミング依存になりやすいので、固定の Delay ではなく
    /// 「余裕のあるタイムアウト + 条件成立で即終了」の形にしています。
    /// </summary>
    public static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(50);
        }

        return condition();
    }

    /// <summary>現在値より大きければ更新する Interlocked な max です。</summary>
    public static void InterlockedMax(ref int location, int value)
    {
        int current = Volatile.Read(ref location);
        while (value > current)
        {
            int original = Interlocked.CompareExchange(ref location, value, current);
            if (original == current)
            {
                return;
            }

            current = original;
        }
    }
}
