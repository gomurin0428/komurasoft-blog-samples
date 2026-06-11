using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text.Json;

namespace KomuraSoft.AdminBroker;

public sealed class ElevationBrokerClient
{
    private readonly string _helperExePath;

    public ElevationBrokerClient(string helperExePath)
    {
        _helperExePath = Path.GetFullPath(helperExePath);

        if (!Path.IsPathRooted(_helperExePath))
        {
            throw new ArgumentException("Helper executable path must be absolute.", nameof(helperExePath));
        }

        if (!File.Exists(_helperExePath))
        {
            throw new FileNotFoundException("Helper executable was not found.", _helperExePath);
        }
    }

    public async Task SetExplorerContextMenuEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        // UAC の昇格プロンプト（Verb = "runas"）と Windows ユーザー SID は Windows 専用のため、
        // 非 Windows では分かりやすい例外で早期に失敗させます。
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "管理者 helper の昇格起動（runas）は Windows 上でのみ使用できます。");
        }

        string pipeName = $"myapp-broker-{Guid.NewGuid():N}";
        int clientPid = Environment.ProcessId;
        string clientSid = GetCurrentUserSid();

        StartHelper(pipeName, clientPid, clientSid);

        using var pipe = new NamedPipeClientStream(
            serverName: ".",
            pipeName: pipeName,
            direction: PipeDirection.InOut,
            options: PipeOptions.Asynchronous);

        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connectCts.CancelAfter(TimeSpan.FromSeconds(30));

        await pipe.ConnectAsync(connectCts.Token);

        BrokerRequest request = new(
            BrokerOperations.SetExplorerContextMenu,
            JsonSerializer.SerializeToElement(
                new SetExplorerContextMenuRequest(enabled),
                BrokerJson.Options));

        await PipeMessageSerializer.WriteAsync(pipe, request, cancellationToken);

        BrokerResponse response = await PipeMessageSerializer.ReadAsync<BrokerResponse>(pipe, cancellationToken);

        if (!response.Success)
        {
            throw new InvalidOperationException(
                $"Admin broker returned an error. Code={response.ErrorCode}, Message={response.Message}");
        }
    }

    [SupportedOSPlatform("windows")]
    private void StartHelper(string pipeName, int clientPid, string clientSid)
    {
        string workingDirectory = Path.GetDirectoryName(_helperExePath)
            ?? throw new InvalidOperationException("Helper executable directory could not be resolved.");

        var startInfo = new ProcessStartInfo
        {
            FileName = _helperExePath,
            Arguments = BuildArguments(pipeName, clientPid, clientSid),
            WorkingDirectory = workingDirectory,
            UseShellExecute = true,
            Verb = "runas"
        };

        try
        {
            _ = Process.Start(startInfo)
                ?? throw new InvalidOperationException("The helper process could not be started.");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            throw new OperationCanceledException("管理者権限の承認がキャンセルされました。", ex);
        }
    }

    [SupportedOSPlatform("windows")]
    private static string GetCurrentUserSid()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        return identity.User?.Value
            ?? throw new InvalidOperationException("Current user SID could not be resolved.");
    }

    private static string BuildArguments(string pipeName, int clientPid, string clientSid)
    {
        return string.Join(
            " ",
            "--pipe",
            QuoteArgument(pipeName),
            "--client-pid",
            clientPid.ToString(CultureInfo.InvariantCulture),
            "--client-sid",
            QuoteArgument(clientSid));
    }

    // このサンプルで渡している pipe 名、PID、SID のような単純な値を前提にした最小実装です。
    // 任意の Windows パスや自由入力文字列を渡す場合は、Windows の argv 解析規則に沿った
    // 専用のエスケープ処理に置き換えてください（記事 10 章）。
    private static string QuoteArgument(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
