using System.Buffers.Binary;
using System.Text.Json;
using Xunit;

namespace KomuraSoft.AdminBroker.Tests;

public class PipeMessageSerializerTests
{
    [Fact]
    public async Task WriteAndRead_BrokerRequest_RoundTrips()
    {
        using var stream = new MemoryStream();
        BrokerRequest request = new(
            BrokerOperations.SetExplorerContextMenu,
            JsonSerializer.SerializeToElement(
                new SetExplorerContextMenuRequest(true),
                BrokerJson.Options));

        await PipeMessageSerializer.WriteAsync(stream, request, CancellationToken.None);
        stream.Position = 0;

        BrokerRequest restored = await PipeMessageSerializer.ReadAsync<BrokerRequest>(stream, CancellationToken.None);

        Assert.Equal(BrokerOperations.SetExplorerContextMenu, restored.Operation);

        SetExplorerContextMenuRequest? payload =
            restored.Payload.Deserialize<SetExplorerContextMenuRequest>(BrokerJson.Options);
        Assert.NotNull(payload);
        Assert.True(payload.Enabled);
    }

    [Fact]
    public async Task WriteAndRead_BrokerResponse_RoundTrips()
    {
        using var stream = new MemoryStream();
        BrokerResponse response = BrokerResponse.Fail("unsupported_operation", "Unsupported operation: install-service");

        await PipeMessageSerializer.WriteAsync(stream, response, CancellationToken.None);
        stream.Position = 0;

        BrokerResponse restored = await PipeMessageSerializer.ReadAsync<BrokerResponse>(stream, CancellationToken.None);

        Assert.False(restored.Success);
        Assert.Equal("unsupported_operation", restored.ErrorCode);
        Assert.Equal("Unsupported operation: install-service", restored.Message);
    }

    [Fact]
    public async Task ReadAsync_OneByteChunks_StillReadsWholeMessage()
    {
        // パイプでも 1 回の ReadAsync が要求バイト数に満たないことはあり得るため、
        // ReadExactAsync が読み切るまでループすることを確認します。
        using var buffer = new MemoryStream();
        BrokerResponse response = BrokerResponse.Ok("Explorer context menu setting was updated.");
        await PipeMessageSerializer.WriteAsync(buffer, response, CancellationToken.None);

        var chunked = new ChunkedReadStream(buffer.ToArray(), maxChunkSize: 1);
        BrokerResponse restored = await PipeMessageSerializer.ReadAsync<BrokerResponse>(chunked, CancellationToken.None);

        Assert.True(restored.Success);
        Assert.Equal("Explorer context menu setting was updated.", restored.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(256 * 1024 + 1)]
    public async Task ReadAsync_InvalidPayloadLength_ThrowsInvalidDataException(int payloadLength)
    {
        byte[] header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, payloadLength);
        using var stream = new MemoryStream(header);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => PipeMessageSerializer.ReadAsync<BrokerResponse>(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_TruncatedHeader_ThrowsEndOfStreamException()
    {
        using var stream = new MemoryStream([0x01, 0x00]);

        await Assert.ThrowsAsync<EndOfStreamException>(
            () => PipeMessageSerializer.ReadAsync<BrokerResponse>(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_TruncatedPayload_ThrowsEndOfStreamException()
    {
        byte[] header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, 10);
        using var stream = new MemoryStream([.. header, (byte)'{', (byte)'}']);

        await Assert.ThrowsAsync<EndOfStreamException>(
            () => PipeMessageSerializer.ReadAsync<BrokerResponse>(stream, CancellationToken.None));
    }

    [Fact]
    public async Task WriteAsync_PayloadTooLarge_ThrowsInvalidDataException()
    {
        using var stream = new MemoryStream();
        string oversized = new('a', 300_000);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => PipeMessageSerializer.WriteAsync(stream, oversized, CancellationToken.None));

        // 不正なフレームを途中まで書き込んでいないこと
        Assert.Equal(0, stream.Length);
    }
}
