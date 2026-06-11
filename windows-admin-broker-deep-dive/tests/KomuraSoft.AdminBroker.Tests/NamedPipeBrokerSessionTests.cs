using System.IO.Pipes;
using System.Text.Json;
using Xunit;

namespace KomuraSoft.AdminBroker.Tests;

/// <summary>
/// 実際の名前付きパイプ上で「1 回の要求、1 回の応答」プロトコルを検証します。
/// 名前付きパイプの IPC 部分は .NET なら Linux でも動作するため、このテストは Linux でも実行できます
/// （明示 ACL と接続元 PID 検証は Windows 専用のため helper 側にあり、ここでは対象外です）。
/// </summary>
public class NamedPipeBrokerSessionTests
{
    [Fact]
    public async Task SetExplorerContextMenu_OverNamedPipe_RoundTrips()
    {
        var registrar = new RecordingRegistrar();
        BrokerRequest request = new(
            BrokerOperations.SetExplorerContextMenu,
            JsonSerializer.SerializeToElement(
                new SetExplorerContextMenuRequest(true),
                BrokerJson.Options));

        BrokerResponse response = await ExchangeOverNamedPipeAsync(request, registrar);

        Assert.True(response.Success);
        Assert.Equal([true], registrar.Calls);
    }

    [Fact]
    public async Task UnsupportedOperation_OverNamedPipe_IsRejected()
    {
        var registrar = new RecordingRegistrar();
        BrokerRequest request = new(
            "add-firewall-rule",
            JsonSerializer.SerializeToElement(new { port = 8080 }, BrokerJson.Options));

        BrokerResponse response = await ExchangeOverNamedPipeAsync(request, registrar);

        Assert.False(response.Success);
        Assert.Equal("unsupported_operation", response.ErrorCode);
        Assert.Empty(registrar.Calls);
    }

    private static async Task<BrokerResponse> ExchangeOverNamedPipeAsync(
        BrokerRequest request,
        IExplorerContextMenuRegistrar registrar)
    {
        string pipeName = $"komurasoft-adminbroker-test-{Guid.NewGuid():N}";
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

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
            await server.WaitForConnectionAsync(cts.Token);
            return await BrokerSession.RunAsync(server, registrar, cts.Token);
        });

        await client.ConnectAsync(cts.Token);
        await PipeMessageSerializer.WriteAsync(client, request, cts.Token);

        BrokerResponse response = await PipeMessageSerializer.ReadAsync<BrokerResponse>(client, cts.Token);

        BrokerResponse serverResponse = await serverTask;
        Assert.Equal(serverResponse.Success, response.Success);

        return response;
    }
}
