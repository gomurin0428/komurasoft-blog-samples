namespace KomuraSoft.AdminBroker.Tests;

/// <summary>
/// 呼び出しを記録するだけの registrar です（レジストリの代役）。
/// </summary>
public sealed class RecordingRegistrar : IExplorerContextMenuRegistrar
{
    public List<bool> Calls { get; } = [];

    public void Apply(bool enabled) => Calls.Add(enabled);
}

/// <summary>
/// 管理者操作の実体が失敗したケースを再現する registrar です。
/// </summary>
public sealed class ThrowingRegistrar : IExplorerContextMenuRegistrar
{
    public void Apply(bool enabled) =>
        throw new InvalidOperationException("Registry operation failed (test).");
}
