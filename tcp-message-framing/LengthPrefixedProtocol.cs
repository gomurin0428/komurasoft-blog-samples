using System.Buffers.Binary;

namespace TcpMessageFraming;

/// <summary>
/// 長さプレフィックス方式（[4バイトの本文長][本文]）のフレームを読み取る。
/// </summary>
public static class LengthPrefixedProtocol
{
    private const int HeaderSize = 4;
    private const int MaxPayloadSize = 1024 * 1024; // 1 MiB。用途に合わせて決める

    public static async ValueTask<byte[]?> ReadFrameAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        byte[] header = new byte[HeaderSize];

        int headerBytes = await ReadUntilFullOrEndAsync(
            stream,
            header,
            cancellationToken);

        if (headerBytes == 0)
        {
            // フレームの途中ではなく、次のフレーム開始前に相手が正常終了した
            return null;
        }

        if (headerBytes != HeaderSize)
        {
            throw new EndOfStreamException("Frame header was truncated.");
        }

        int payloadLength = BinaryPrimitives.ReadInt32BigEndian(header);

        if (payloadLength < 0 || payloadLength > MaxPayloadSize)
        {
            throw new InvalidDataException(
                $"Invalid payload length: {payloadLength} bytes.");
        }

        byte[] payload = new byte[payloadLength];

        int payloadBytes = await ReadUntilFullOrEndAsync(
            stream,
            payload,
            cancellationToken);

        if (payloadBytes != payloadLength)
        {
            throw new EndOfStreamException("Frame payload was truncated.");
        }

        return payload;
    }

    private static async ValueTask<int> ReadUntilFullOrEndAsync(
        Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        int totalRead = 0;

        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(
                buffer[totalRead..],
                cancellationToken);

            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }
}
