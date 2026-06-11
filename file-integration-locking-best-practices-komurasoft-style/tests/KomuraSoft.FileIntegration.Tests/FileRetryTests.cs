using Xunit;

namespace KomuraSoft.FileIntegration.Tests;

public class FileRetryTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Execute_SucceedsAfterTransientLockIsReleased()
    {
        // 他プロセスが FileShare.None で掴んでいる間は IOException になるが、
        // 解放後のリトライで読めるようになる、という実挙動を再現する
        string path = _temp.Combine("shared.csv");
        File.WriteAllText(path, "id,amount\n1,100\n");

        var holder = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        var releaseTimer = new Timer(_ => holder.Dispose(), null,
            dueTime: TimeSpan.FromMilliseconds(150), period: Timeout.InfiniteTimeSpan);

        try
        {
            string content = FileRetry.Execute(
                () =>
                {
                    using var stream = new FileStream(
                        path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                },
                maxAttempts: 20,
                delay: TimeSpan.FromMilliseconds(50));

            Assert.Equal("id,amount\n1,100\n", content);
        }
        finally
        {
            releaseTimer.Dispose();
        }
    }

    [Fact]
    public void Execute_ThrowsAfterExhaustingAttempts()
    {
        int attempts = 0;

        Assert.Throws<IOException>(() =>
            FileRetry.Execute<string>(
                () =>
                {
                    attempts++;
                    throw new IOException("still locked");
                },
                maxAttempts: 3,
                delay: TimeSpan.FromMilliseconds(1)));

        Assert.Equal(3, attempts); // 指定回数で諦めて元の例外を伝える
    }

    [Fact]
    public void Execute_DoesNotRetryNonIoExceptions()
    {
        int attempts = 0;

        Assert.Throws<InvalidDataException>(() =>
            FileRetry.Execute<string>(
                () =>
                {
                    attempts++;
                    throw new InvalidDataException("broken payload");
                },
                maxAttempts: 5,
                delay: TimeSpan.FromMilliseconds(1)));

        Assert.Equal(1, attempts); // 一時的でないエラーはリトライしない
    }
}
