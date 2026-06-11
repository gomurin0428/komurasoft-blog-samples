using Microsoft.Win32;
using KomuraSoft.AdminBroker;

namespace MyApp.AdminBroker;

internal static class ExplorerContextMenuRegistration
{
    private const string MenuKeyPath = @"SOFTWARE\Classes\*\shell\MyApp.Open";
    private const string CommandKeyPath = @"SOFTWARE\Classes\*\shell\MyApp.Open\command";
    private const string MenuText = "Open with MyApp";
    private const string ClientExecutableName = "MyApp.exe";

    public static void Apply(bool enabled)
    {
        string clientExePath = ResolveClientExecutablePath();

        using RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, GetRegistryView());

        if (enabled)
        {
            using RegistryKey menuKey = hklm.CreateSubKey(MenuKeyPath)
                ?? throw new InvalidOperationException($"Failed to create registry key: {MenuKeyPath}");

            menuKey.SetValue(null, MenuText, RegistryValueKind.String);
            menuKey.SetValue("Icon", $"\"{clientExePath}\",0", RegistryValueKind.String);

            using RegistryKey commandKey = hklm.CreateSubKey(CommandKeyPath)
                ?? throw new InvalidOperationException($"Failed to create registry key: {CommandKeyPath}");

            commandKey.SetValue(null, $"\"{clientExePath}\" \"%1\"", RegistryValueKind.String);
        }
        else
        {
            hklm.DeleteSubKeyTree(@"SOFTWARE\Classes\*\shell\MyApp.Open", throwOnMissingSubKey: false);
        }
    }

    private static string ResolveClientExecutablePath()
    {
        string clientExePath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, ClientExecutableName));

        if (!File.Exists(clientExePath))
        {
            throw new FileNotFoundException("Client executable was not found.", clientExePath);
        }

        return clientExePath;
    }

    private static RegistryView GetRegistryView()
    {
        return Environment.Is64BitOperatingSystem
            ? RegistryView.Registry64
            : RegistryView.Registry32;
    }
}

/// <summary>
/// レジストリを実際に操作する registrar です。
/// プロトコル部分（<see cref="BrokerSession"/>）とは分離し、管理者操作の実体だけを helper 側に置きます。
/// </summary>
internal sealed class RegistryExplorerContextMenuRegistrar : IExplorerContextMenuRegistrar
{
    public void Apply(bool enabled) => ExplorerContextMenuRegistration.Apply(enabled);
}
