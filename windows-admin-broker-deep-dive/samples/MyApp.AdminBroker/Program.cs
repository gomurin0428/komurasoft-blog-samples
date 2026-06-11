using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using KomuraSoft.AdminBroker;

namespace MyApp.AdminBroker;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // pipe の明示 ACL（PipeSecurity / NamedPipeServerStreamAcl）、接続元 PID の検証、
        // HKLM のレジストリ操作はいずれも Windows 専用です（ビルドは Linux でも可能です）。
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("この helper は Windows 専用です。");
            Console.WriteLine("名前付きパイプの明示 ACL と HKLM レジストリ操作は Windows 上で実行してください。");
            Console.WriteLine();
            Console.WriteLine("Windows での確認手順は README.md の「Windows での確認手順」を参照してください。");
            return 1;
        }

        BrokerLaunchOptions options = BrokerLaunchOptions.Parse(args);

        using var brokerCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using NamedPipeServerStream pipe = CreatePipeServer(options);

        await pipe.WaitForConnectionAsync(brokerCts.Token);

        VerifyClientProcessId(pipe, options.ExpectedClientProcessId);

        BrokerResponse response = await BrokerSession.RunAsync(
            pipe,
            new RegistryExplorerContextMenuRegistrar(),
            brokerCts.Token);

        return response.Success ? 0 : 2;
    }

    private static NamedPipeServerStream CreatePipeServer(BrokerLaunchOptions options)
    {
        var pipeSecurity = new PipeSecurity();
        var clientSid = new SecurityIdentifier(options.ClientUserSid);
        SecurityIdentifier helperSid = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("Helper user SID could not be resolved.");

        pipeSecurity.AddAccessRule(new PipeAccessRule(
            clientSid,
            PipeAccessRights.ReadWrite,
            AccessControlType.Allow));

        pipeSecurity.AddAccessRule(new PipeAccessRule(
            helperSid,
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        pipeSecurity.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            options.PipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            transmissionMode: PipeTransmissionMode.Byte,
            options: PipeOptions.Asynchronous | PipeOptions.WriteThrough,
            inBufferSize: 0,
            outBufferSize: 0,
            pipeSecurity: pipeSecurity);
    }

    private static void VerifyClientProcessId(NamedPipeServerStream pipe, int expectedClientProcessId)
    {
        if (!GetNamedPipeClientProcessId(
                pipe.SafePipeHandle.DangerousGetHandle(),
                out uint actualClientProcessId))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (actualClientProcessId != (uint)expectedClientProcessId)
        {
            throw new InvalidOperationException(
                $"Unexpected pipe client PID. Expected={expectedClientProcessId}, Actual={actualClientProcessId}");
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientProcessId(
        IntPtr pipe,
        out uint clientProcessId);
}
