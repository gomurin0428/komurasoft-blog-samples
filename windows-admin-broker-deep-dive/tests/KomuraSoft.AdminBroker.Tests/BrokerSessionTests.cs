using System.Text.Json;
using Xunit;

namespace KomuraSoft.AdminBroker.Tests;

public class BrokerSessionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RunAsync_SetExplorerContextMenu_CallsRegistrarAndRespondsOk(bool enabled)
    {
        var registrar = new RecordingRegistrar();
        BrokerRequest request = new(
            BrokerOperations.SetExplorerContextMenu,
            JsonSerializer.SerializeToElement(
                new SetExplorerContextMenuRequest(enabled),
                BrokerJson.Options));

        (BrokerResponse returned, BrokerResponse written) = await RunSessionAsync(request, registrar);

        Assert.True(returned.Success);
        Assert.Null(returned.ErrorCode);
        Assert.Equal([enabled], registrar.Calls);

        // クライアントに書き戻された応答も同じ内容であること
        Assert.True(written.Success);
        Assert.Equal(returned.Message, written.Message);
    }

    [Fact]
    public async Task RunAsync_UnsupportedOperation_RespondsFailWithoutExecutingAnything()
    {
        // allowlist にない operation は dispatch されない（記事 5.1 章・12 章）
        var registrar = new RecordingRegistrar();
        BrokerRequest request = new(
            "install-service",
            JsonSerializer.SerializeToElement(new { serviceName = "EvilService" }, BrokerJson.Options));

        (BrokerResponse returned, _) = await RunSessionAsync(request, registrar);

        Assert.False(returned.Success);
        Assert.Equal("unsupported_operation", returned.ErrorCode);
        Assert.Empty(registrar.Calls);
    }

    [Fact]
    public async Task RunAsync_InvalidPayload_RespondsInvalidPayload()
    {
        // 型付き request に合わない payload は helper 側の再検証で弾く（記事 12 章）
        var registrar = new RecordingRegistrar();
        BrokerRequest request = new(
            BrokerOperations.SetExplorerContextMenu,
            JsonSerializer.SerializeToElement("not-an-object", BrokerJson.Options));

        (BrokerResponse returned, _) = await RunSessionAsync(request, registrar);

        Assert.False(returned.Success);
        Assert.Equal("invalid_payload", returned.ErrorCode);
        Assert.Empty(registrar.Calls);
    }

    [Fact]
    public async Task RunAsync_RegistrarThrows_RespondsBrokerFailure()
    {
        BrokerRequest request = new(
            BrokerOperations.SetExplorerContextMenu,
            JsonSerializer.SerializeToElement(
                new SetExplorerContextMenuRequest(true),
                BrokerJson.Options));

        (BrokerResponse returned, BrokerResponse written) = await RunSessionAsync(request, new ThrowingRegistrar());

        Assert.False(returned.Success);
        Assert.Equal("broker_failure", returned.ErrorCode);

        // 失敗してもクライアントには必ず応答が返ること
        Assert.False(written.Success);
        Assert.Equal("broker_failure", written.ErrorCode);
    }

    /// <summary>
    /// MemoryStream を擬似的な双方向パイプとして使い、1 セッション分
    /// （要求の読み取り → dispatch → 応答の書き込み）を実行します。
    /// </summary>
    private static async Task<(BrokerResponse Returned, BrokerResponse Written)> RunSessionAsync(
        BrokerRequest request,
        IExplorerContextMenuRegistrar registrar)
    {
        using var stream = new MemoryStream();
        await PipeMessageSerializer.WriteAsync(stream, request, CancellationToken.None);
        long requestLength = stream.Position;
        stream.Position = 0;

        BrokerResponse returned = await BrokerSession.RunAsync(stream, registrar, CancellationToken.None);

        stream.Position = requestLength;
        BrokerResponse written = await PipeMessageSerializer.ReadAsync<BrokerResponse>(stream, CancellationToken.None);

        return (returned, written);
    }
}
