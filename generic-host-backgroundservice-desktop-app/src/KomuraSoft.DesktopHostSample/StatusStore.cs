namespace DesktopHostSample;

public sealed class StatusStore
{
    private readonly object _gate = new();
    private DeviceStatus _current = DeviceStatus.Empty;

    public DeviceStatus Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public void Update(DeviceStatus next)
    {
        lock (_gate)
        {
            _current = next;
        }
    }
}

public sealed record DeviceStatus(string Message)
{
    public static readonly DeviceStatus Empty = new("No Data");
}
