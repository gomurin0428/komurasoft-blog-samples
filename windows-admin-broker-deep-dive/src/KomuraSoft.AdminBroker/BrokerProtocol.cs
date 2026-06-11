using System.Buffers.Binary;
using System.Text.Json;

namespace KomuraSoft.AdminBroker;

public static class BrokerJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

public static class BrokerOperations
{
    public const string SetExplorerContextMenu = "set-explorer-context-menu";
}

public sealed record BrokerRequest(string Operation, JsonElement Payload);

public sealed record BrokerResponse(bool Success, string? ErrorCode, string? Message)
{
    public static BrokerResponse Ok(string? message = null) => new(true, null, message);

    public static BrokerResponse Fail(string errorCode, string message) =>
        new(false, errorCode, message);
}

public sealed record SetExplorerContextMenuRequest(bool Enabled);

public static class PipeMessageSerializer
{
    private const int MaxPayloadBytes = 256 * 1024;

    public static async Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value, BrokerJson.Options);
        if (payload.Length > MaxPayloadBytes)
        {
            throw new InvalidDataException($"Payload is too large: {payload.Length} bytes.");
        }

        byte[] header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);

        await stream.WriteAsync(header.AsMemory(0, header.Length), cancellationToken);
        await stream.WriteAsync(payload.AsMemory(0, payload.Length), cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async Task<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken)
    {
        byte[] header = await ReadExactAsync(stream, sizeof(int), cancellationToken);
        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(header);

        if (payloadLength <= 0 || payloadLength > MaxPayloadBytes)
        {
            throw new InvalidDataException($"Invalid payload length: {payloadLength}");
        }

        byte[] payload = await ReadExactAsync(stream, payloadLength, cancellationToken);

        return JsonSerializer.Deserialize<T>(payload, BrokerJson.Options)
            ?? throw new InvalidDataException($"Failed to deserialize {typeof(T).FullName}.");
    }

    private static async Task<byte[]> ReadExactAsync(Stream stream, int length, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[length];
        int offset = 0;

        while (offset < length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset, length - offset), cancellationToken);
            if (read == 0)
            {
                throw new EndOfStreamException("Pipe was closed before the expected number of bytes was read.");
            }

            offset += read;
        }

        return buffer;
    }
}
