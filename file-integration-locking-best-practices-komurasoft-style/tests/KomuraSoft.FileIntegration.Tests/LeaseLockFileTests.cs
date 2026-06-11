using Xunit;

namespace KomuraSoft.FileIntegration.Tests;

public class LeaseLockFileTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    private string LockPath => _temp.Combine("nightly-batch.lock.json");

    [Fact]
    public void TryAcquire_WritesOwnershipInformation()
    {
        using LeaseLockFile? lease = LeaseLockFile.TryAcquire(
            LockPath, "worker-1", TimeSpan.FromMinutes(5));

        Assert.NotNull(lease);
        LeaseInfo? onDisk = LeaseLockFile.TryRead(LockPath);
        Assert.NotNull(onDisk);
        Assert.Equal("worker-1", onDisk.OwnerId);              // 誰の lock か分かる
        Assert.Equal(Environment.ProcessId, onDisk.Pid);
        Assert.True(onDisk.ExpiresAt > onDisk.AcquiredAt);     // 有効期限付き
    }

    [Fact]
    public void TryAcquire_SecondOwnerFailsWhileLeaseIsValid()
    {
        using LeaseLockFile? first = LeaseLockFile.TryAcquire(
            LockPath, "worker-1", TimeSpan.FromMinutes(5));
        LeaseLockFile? second = LeaseLockFile.TryAcquire(
            LockPath, "worker-2", TimeSpan.FromMinutes(5));

        Assert.NotNull(first);
        Assert.Null(second); // 期限内の lock は奪えない
    }

    [Fact]
    public void TryAcquire_TakesOverStaleLease()
    {
        // 記事 2.3 章「stale lock で全員が止まる」の対策。
        // worker-1 が異常終了して heartbeat が止まった（= Release されない）状況を再現する。
        LeaseLockFile? crashed = LeaseLockFile.TryAcquire(
            LockPath, "worker-1", TimeSpan.FromMilliseconds(50));
        Assert.NotNull(crashed);

        Thread.Sleep(200); // lease の期限切れを待つ

        using LeaseLockFile? successor = LeaseLockFile.TryAcquire(
            LockPath, "worker-2", TimeSpan.FromMinutes(5));

        Assert.NotNull(successor);
        Assert.Equal("worker-2", LeaseLockFile.TryRead(LockPath)!.OwnerId);
    }

    [Fact]
    public void Renew_ExtendsExpirationAndUpdatesHeartbeat()
    {
        using LeaseLockFile? lease = LeaseLockFile.TryAcquire(
            LockPath, "worker-1", TimeSpan.FromMinutes(5));
        Assert.NotNull(lease);
        DateTimeOffset originalExpiry = lease.Info.ExpiresAt;

        Thread.Sleep(50);
        lease.Renew();

        LeaseInfo onDisk = LeaseLockFile.TryRead(LockPath)!;
        Assert.True(onDisk.ExpiresAt > originalExpiry);
        Assert.True(onDisk.HeartbeatAt >= onDisk.AcquiredAt);
    }

    [Fact]
    public void Release_DeletesLockFile()
    {
        LeaseLockFile? lease = LeaseLockFile.TryAcquire(
            LockPath, "worker-1", TimeSpan.FromMinutes(5));
        Assert.NotNull(lease);

        lease.Release();

        Assert.False(File.Exists(LockPath));
    }

    [Fact]
    public void Release_DoesNotDeleteLockTakenOverByAnotherOwner()
    {
        // 削除は「原則として作成者だけ」（記事 4.4 章）。
        // 自分の lease が他者に引き取られた後に Release しても、他者の lock を消してはいけない。
        LeaseLockFile? expired = LeaseLockFile.TryAcquire(
            LockPath, "worker-1", TimeSpan.FromMilliseconds(50));
        Assert.NotNull(expired);
        Thread.Sleep(200);

        using LeaseLockFile? successor = LeaseLockFile.TryAcquire(
            LockPath, "worker-2", TimeSpan.FromMinutes(5));
        Assert.NotNull(successor);

        expired.Release(); // 遅れて戻ってきた worker-1 が後始末を試みる

        LeaseInfo? onDisk = LeaseLockFile.TryRead(LockPath);
        Assert.NotNull(onDisk);
        Assert.Equal("worker-2", onDisk.OwnerId); // worker-2 の lock は無事
    }

    [Fact]
    public void TryAcquire_TakesOverCorruptedLockFile()
    {
        File.WriteAllText(LockPath, "{ not valid json"); // 壊れた lock file

        using LeaseLockFile? lease = LeaseLockFile.TryAcquire(
            LockPath, "worker-1", TimeSpan.FromMinutes(5));

        Assert.NotNull(lease); // 読めない lock は stale 扱いで引き取れる
    }
}
