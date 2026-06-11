using Xunit;

namespace KomuraSoft.FileWatching.Tests;

public class AtomicClaimTests
{
    [Fact]
    public void TryClaimByRename_OnlyOneWorkerWins()
    {
        using var dir = new TestDirectory();
        string bundlePath = Path.Combine(dir.IncomingDir, "order-001");
        Directory.CreateDirectory(bundlePath);
        Directory.CreateDirectory(dir.ProcessingDir("worker1"));
        Directory.CreateDirectory(dir.ProcessingDir("worker2"));

        bool first = AtomicClaim.TryClaimByRename(
            bundlePath,
            Path.Combine(dir.ProcessingDir("worker1"), "order-001"));

        bool second = AtomicClaim.TryClaimByRename(
            bundlePath,
            Path.Combine(dir.ProcessingDir("worker2"), "order-001"));

        Assert.True(first);
        Assert.False(second);
        Assert.True(Directory.Exists(Path.Combine(dir.ProcessingDir("worker1"), "order-001")));
        Assert.False(Directory.Exists(Path.Combine(dir.ProcessingDir("worker2"), "order-001")));
    }

    [Fact]
    public async Task TryClaimByRename_ConcurrentClaims_ExactlyOneSucceeds()
    {
        using var dir = new TestDirectory();
        string bundlePath = Path.Combine(dir.IncomingDir, "order-002");
        Directory.CreateDirectory(bundlePath);

        const int workerCount = 8;

        for (int i = 0; i < workerCount; i++)
        {
            Directory.CreateDirectory(dir.ProcessingDir($"worker{i}"));
        }

        using var start = new SemaphoreSlim(0, workerCount);

        Task<bool>[] claims = Enumerable.Range(0, workerCount)
            .Select(i => Task.Run(async () =>
            {
                await start.WaitAsync();
                return AtomicClaim.TryClaimByRename(
                    bundlePath,
                    Path.Combine(dir.ProcessingDir($"worker{i}"), "order-002"));
            }))
            .ToArray();

        start.Release(workerCount);
        bool[] results = await Task.WhenAll(claims);

        Assert.Equal(1, results.Count(claimed => claimed));
    }
}
