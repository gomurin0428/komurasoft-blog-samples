namespace DesktopHostSample;

/// <summary>
/// 外部状態（装置 / サーバーなど）を 1 回読み取る scoped サービスです。
/// 記事ではインターフェイス名のみ登場するため、シグネチャをここで補っています。
/// </summary>
public interface IDeviceStatusReader
{
    Task<DeviceStatus> ReadAsync(CancellationToken cancellationToken);
}
