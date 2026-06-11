using System.Buffers.Binary;

namespace KomuraSoft.TcpFraming;

/// <summary>
/// 長さプレフィックス方式（[4バイトの本文長][本文]）のフレームを書き込みます。
/// </summary>
public static class LengthPrefixedProtocolWriter
{
    private const int HeaderSize = 4;
    private const int MaxPayloadSize = LengthPrefixedProtocol.MaxPayloadSize;

    public static async ValueTask WriteFrameAsync(
        Stream stream,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        if (payload.Length > MaxPayloadSize)
        {
            throw new InvalidDataException(
                $"Payload is too large: {payload.Length} bytes.");
        }

        byte[] header = new byte[HeaderSize];
        BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);

        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
    }
}
