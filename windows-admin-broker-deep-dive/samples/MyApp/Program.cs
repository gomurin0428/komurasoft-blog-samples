using System.IO.Pipes;
using System.Text.Json;
using KomuraSoft.AdminBroker;

// asInvoker の UI アプリ側に相当するコンソールデモです。
//
// - Windows では、管理者 helper（MyApp.AdminBroker.exe）を runas で起動し、
//   名前付きパイプ経由で「Explorer 右クリックメニューの登録 / 解除」を依頼します（記事 10 章・14 章）。
// - 非 Windows では UAC 昇格（runas）が存在しないため、同一プロセス内に broker セッションを立てて、
//   名前付きパイプの IPC プロトコル部分（型付き要求 / allowlist dispatch / 応答）だけを実演します。

if (OperatingSystem.IsWindows())
{
    bool enabled = !args.Contains("--disable");

    // helper EXE は絶対パスで固定解決する（記事 5.2 章）。
    // 開発レイアウトでは helper の出力先が異なるため、両方を同じフォルダへ publish してから実行してください
    // （README.md の「Windows での確認手順」を参照）。
    var broker = new ElevationBrokerClient(
        Path.Combine(AppContext.BaseDirectory, "MyApp.AdminBroker.exe"));

    try
    {
        await broker.SetExplorerContextMenuEnabledAsync(enabled);
        Console.WriteLine($"[ui]     Explorer 右クリックメニューの登録状態を更新しました: enabled={enabled}");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("[ui]     管理者権限の承認がキャンセルされました。設定は変更されていません。");
    }

    return;
}

Console.WriteLine("この環境は Windows ではないため、UAC 昇格（runas）と helper の起動は行いません。");
Console.WriteLine("代わりに、helper との IPC プロトコル部分を同一プロセス内の名前付きパイプで実演します。");
Console.WriteLine();

var registrar = new ConsoleExplorerContextMenuRegistrar();

// 1. 固定 operation（set-explorer-context-menu）の登録 / 解除
foreach (bool enabled in new[] { true, false })
{
    BrokerRequest request = new(
        BrokerOperations.SetExplorerContextMenu,
        JsonSerializer.SerializeToElement(
            new SetExplorerContextMenuRequest(enabled),
            BrokerJson.Options));

    BrokerResponse response = await ExchangeAsync(request, registrar);
    Console.WriteLine($"[ui]     応答: Success={response.Success}, Message={response.Message}");
    Console.WriteLine();
}

// 2. allowlist にない operation は helper 側で拒否される（記事 5.1 章・12 章）
BrokerRequest unsupported = new(
    "install-service",
    JsonSerializer.SerializeToElement(new { serviceName = "EvilService" }, BrokerJson.Options));

BrokerResponse rejected = await ExchangeAsync(unsupported, registrar);
Console.WriteLine($"[ui]     応答: Success={rejected.Success}, ErrorCode={rejected.ErrorCode}");
Console.WriteLine($"[ui]            Message={rejected.Message}");
Console.WriteLine();
Console.WriteLine("[demo]   done（Windows での確認手順は README.md を参照してください）");

// ElevationBrokerClient の pipe 通信部分と同じ「1 回の要求、1 回の応答」を、
// サーバー側（BrokerSession）とクライアント側を同一プロセスに置いて実行します。
static async Task<BrokerResponse> ExchangeAsync(BrokerRequest request, IExplorerContextMenuRegistrar registrar)
{
    string pipeName = $"myapp-broker-demo-{Guid.NewGuid():N}";

    await using var server = new NamedPipeServerStream(
        pipeName,
        PipeDirection.InOut,
        maxNumberOfServerInstances: 1,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous);

    await using var client = new NamedPipeClientStream(
        serverName: ".",
        pipeName: pipeName,
        direction: PipeDirection.InOut,
        options: PipeOptions.Asynchronous);

    Task<BrokerResponse> serverTask = Task.Run(async () =>
    {
        await server.WaitForConnectionAsync();
        return await BrokerSession.RunAsync(server, registrar, CancellationToken.None);
    });

    await client.ConnectAsync();

    Console.WriteLine($"[ui]     要求: Operation={request.Operation}");
    await PipeMessageSerializer.WriteAsync(client, request, CancellationToken.None);

    BrokerResponse response = await PipeMessageSerializer.ReadAsync<BrokerResponse>(client, CancellationToken.None);

    await serverTask;
    return response;
}

/// <summary>
/// レジストリの代わりにコンソールへ出力する registrar です。
/// Windows の helper では同じ場所に HKLM レジストリ操作（ExplorerContextMenuRegistration）が入ります。
/// </summary>
internal sealed class ConsoleExplorerContextMenuRegistrar : IExplorerContextMenuRegistrar
{
    public void Apply(bool enabled)
    {
        string action = enabled ? "登録" : "解除";
        Console.WriteLine($@"[broker] Explorer 右クリックメニューを{action}します（Windows では HKLM\SOFTWARE\Classes\*\shell\MyApp.Open を操作）");
    }
}
