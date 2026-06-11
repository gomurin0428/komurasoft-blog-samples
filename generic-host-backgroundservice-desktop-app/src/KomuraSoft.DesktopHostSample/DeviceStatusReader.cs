namespace DesktopHostSample;

/// <summary>
/// 実機の代わりに装置 I/O を模擬する <see cref="IDeviceStatusReader"/> の実装です。
/// scoped 登録されるため、ポーリング 1 周期ごとに新しいインスタンスが作られます
/// （実務では DbContext や接続オブジェクトなど scoped な依存を持つ層に相当します）。
/// </summary>
public sealed class DeviceStatusReader : IDeviceStatusReader
{
    private static int _readCount;

    public async Task<DeviceStatus> ReadAsync(CancellationToken cancellationToken)
    {
        // 実際の装置 I/O のつもりで少しだけ待つ
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);

        int count = Interlocked.Increment(ref _readCount);
        return new DeviceStatus($"Device OK (read #{count} at {DateTimeOffset.Now:HH:mm:ss})");
    }
}
