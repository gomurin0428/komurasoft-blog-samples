namespace DesktopHostSample.Tests;

/// <summary>
/// scope ごとに何回 reader が生成・呼び出されたかを singleton 側で数えるための記録係です。
/// </summary>
public sealed class ReadProbe
{
    private int _instanceCount;
    private int _readCount;

    public int InstanceCount => Volatile.Read(ref _instanceCount);

    public int ReadCount => Volatile.Read(ref _readCount);

    public int NextInstanceNumber() => Interlocked.Increment(ref _instanceCount);

    public int NextReadNumber() => Interlocked.Increment(ref _readCount);
}

/// <summary>
/// テスト用の <see cref="IDeviceStatusReader"/>。即座に固定の状態を返します。
/// </summary>
public sealed class FakeDeviceStatusReader : IDeviceStatusReader
{
    private readonly ReadProbe _probe;
    private readonly int _instanceNumber;

    public FakeDeviceStatusReader(ReadProbe probe)
    {
        _probe = probe;
        _instanceNumber = probe.NextInstanceNumber();
    }

    public Task<DeviceStatus> ReadAsync(CancellationToken cancellationToken)
    {
        int readNumber = _probe.NextReadNumber();
        return Task.FromResult(new DeviceStatus($"read-{readNumber} (instance {_instanceNumber})"));
    }
}

/// <summary>
/// 最初の 1 回だけ例外を投げ、以降は成功するテスト用 reader。
/// ポーリングループが 1 回の失敗で死なないことの確認に使います。
/// </summary>
public sealed class FlakyDeviceStatusReader : IDeviceStatusReader
{
    private readonly ReadProbe _probe;

    public FlakyDeviceStatusReader(ReadProbe probe)
    {
        _probe = probe;
        _probe.NextInstanceNumber();
    }

    public Task<DeviceStatus> ReadAsync(CancellationToken cancellationToken)
    {
        int readNumber = _probe.NextReadNumber();
        if (readNumber == 1)
        {
            throw new InvalidOperationException("Simulated device failure.");
        }

        return Task.FromResult(new DeviceStatus($"recovered (read-{readNumber})"));
    }
}
