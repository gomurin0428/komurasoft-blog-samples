using System.Text.Json;

namespace KomuraSoft.AdminBroker;

/// <summary>
/// 管理者操作の実体（レジストリ書き込みなど）を helper 側に閉じ込めるための抽象です。
/// プロトコル部分（受信・dispatch・応答）を OS 非依存にして、Linux 上でもテストできるようにします。
/// </summary>
public interface IExplorerContextMenuRegistrar
{
    void Apply(bool enabled);
}

/// <summary>
/// helper 側の「1 回の要求、1 回の応答」セッションです（記事 12 章の dispatch 部分）。
/// 接続済みのストリームから型付きの要求を読み、operation 名の allowlist で dispatch し、応答を書き戻します。
/// </summary>
public static class BrokerSession
{
    public static async Task<BrokerResponse> RunAsync(
        Stream pipe,
        IExplorerContextMenuRegistrar registrar,
        CancellationToken cancellationToken)
    {
        BrokerRequest request = await PipeMessageSerializer.ReadAsync<BrokerRequest>(pipe, cancellationToken);
        BrokerResponse response = await DispatchAsync(request, registrar);

        await PipeMessageSerializer.WriteAsync(pipe, response, cancellationToken);

        return response;
    }

    private static Task<BrokerResponse> DispatchAsync(BrokerRequest request, IExplorerContextMenuRegistrar registrar)
    {
        try
        {
            return request.Operation switch
            {
                BrokerOperations.SetExplorerContextMenu => HandleSetExplorerContextMenuAsync(request.Payload, registrar),
                _ => Task.FromResult(
                    BrokerResponse.Fail(
                        "unsupported_operation",
                        $"Unsupported operation: {request.Operation}"))
            };
        }
        catch (JsonException ex)
        {
            return Task.FromResult(BrokerResponse.Fail("invalid_payload", ex.Message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(BrokerResponse.Fail("broker_failure", ex.Message));
        }
    }

    private static Task<BrokerResponse> HandleSetExplorerContextMenuAsync(
        JsonElement payload,
        IExplorerContextMenuRegistrar registrar)
    {
        SetExplorerContextMenuRequest request = payload.Deserialize<SetExplorerContextMenuRequest>(BrokerJson.Options)
            ?? throw new JsonException("Payload could not be parsed.");

        registrar.Apply(request.Enabled);
        return Task.FromResult(BrokerResponse.Ok("Explorer context menu setting was updated."));
    }
}
