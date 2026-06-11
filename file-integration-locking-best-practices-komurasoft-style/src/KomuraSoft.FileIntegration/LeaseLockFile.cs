using System.Text.Json;
using System.Text.Json.Serialization;

namespace KomuraSoft.FileIntegration;

/// <summary>
/// lock file の中身（記事 4.4 章）。
/// 単なる空ファイルではなく「誰が・いつまで」を持つ有効期限付きの所有情報にします。
/// </summary>
public sealed record LeaseInfo(
    string OwnerId,
    string Host,
    int Pid,
    DateTimeOffset AcquiredAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset HeartbeatAt)
{
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}

/// <summary>
/// lease 方式の lock file（記事 4.4 章）。
/// - 作成は FileMode.CreateNew による原子的作成（Exists -> Create の二段階チェックはしない。記事 3.1 章）
/// - 期限切れ（stale）の lock は後続が引き取れるので、全員が止まる事故（記事 2.3 章）を避けられる
/// - 削除（解放）は原則として作成者だけが行う
/// </summary>
public sealed class LeaseLockFile : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _lockPath;
    private readonly TimeSpan _leaseDuration;
    private bool _released;

    public LeaseInfo Info { get; private set; }

    private LeaseLockFile(string lockPath, TimeSpan leaseDuration, LeaseInfo info)
    {
        _lockPath = lockPath;
        _leaseDuration = leaseDuration;
        Info = info;
    }

    /// <summary>
    /// lock の取得を試みます。取得できなければ null を返します（待ちません）。
    /// 既存の lock が期限切れなら、stale と判定して引き取りを試みます。
    /// </summary>
    public static LeaseLockFile? TryAcquire(string lockPath, string ownerId, TimeSpan leaseDuration)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var info = new LeaseInfo(
            OwnerId: ownerId,
            Host: Environment.MachineName,
            Pid: Environment.ProcessId,
            AcquiredAt: now,
            ExpiresAt: now + leaseDuration,
            HeartbeatAt: now);

        // 1. まず「無ければ作る」を 1 操作で試す
        if (TryCreateNew(lockPath, info))
        {
            return new LeaseLockFile(lockPath, leaseDuration, info);
        }

        // 2. 既存の lock がある。期限内なら諦める。
        LeaseInfo? existing = TryRead(lockPath);
        if (existing is not null && !existing.IsExpired(now))
        {
            return null;
        }

        // 3. stale（期限切れ or 読めない）の lock は削除して引き取りを試みる。
        //    削除から作成までの間に他者が割り込んでも、作成は CreateNew なので二重取得にはならない。
        try
        {
            File.Delete(lockPath);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        return TryCreateNew(lockPath, info)
            ? new LeaseLockFile(lockPath, leaseDuration, info)
            : null;
    }

    /// <summary>
    /// heartbeat を打って有効期限を延長します。
    /// 更新が止まったことを、後続ワーカーの stale 判定の材料にします。
    /// </summary>
    public void Renew()
    {
        ThrowIfReleased();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Info = Info with
        {
            ExpiresAt = now + _leaseDuration,
            HeartbeatAt = now,
        };

        // 更新も temp -> replace で原子的に行い、途中状態の lock file を見せない
        AtomicFilePublisher.Publish(_lockPath, stream =>
            JsonSerializer.Serialize(stream, Info, JsonOptions));
    }

    /// <summary>
    /// lock を解放します。削除するのは作成者（自分の ownerId が入っている場合）だけです。
    /// 他者に引き取られた後の lock を消してしまわないための防御です。
    /// </summary>
    public void Release()
    {
        if (_released)
        {
            return;
        }

        _released = true;

        LeaseInfo? current = TryRead(_lockPath);
        if (current is not null && current.OwnerId == Info.OwnerId)
        {
            File.Delete(_lockPath);
        }
    }

    public void Dispose() => Release();

    /// <summary>lock file を読みます。壊れている・消えた場合は null を返します。</summary>
    public static LeaseInfo? TryRead(string lockPath)
    {
        try
        {
            using var stream = new FileStream(lockPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return JsonSerializer.Deserialize<LeaseInfo>(stream, JsonOptions);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryCreateNew(string lockPath, LeaseInfo info)
    {
        try
        {
            using var stream = new FileStream(
                lockPath,
                FileMode.CreateNew,   // 原子的作成。Exists の事前確認はしない
                FileAccess.Write,
                FileShare.None);
            JsonSerializer.Serialize(stream, info, JsonOptions);
            return true;
        }
        catch (IOException)
        {
            return false; // 既に誰かが持っている
        }
    }

    private void ThrowIfReleased()
    {
        ObjectDisposedException.ThrowIf(_released, this);
    }
}
