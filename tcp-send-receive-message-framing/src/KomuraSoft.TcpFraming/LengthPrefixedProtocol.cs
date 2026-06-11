using System.Buffers.Binary;

namespace KomuraSoft.TcpFraming;

/// <summary>
/// 長さプレフィックス方式（[4バイトの本文長][本文]）のフレームを読み取ります。
/// 本文長は big-endian の int として扱います。
/// </summary>
public static class LengthPrefixedProtocol
{
    private const int HeaderSize = 4;
    public const int MaxPayloadSize = 1024 * 1024; // 1 MiB。用途に合わせて決める

    /// <summary>
    /// ストリームから 1 フレーム読み取ります。
    /// フレーム開始前に相手が正常終了した場合は null を返します。
    /// フレームの途中で切断された場合は <see cref="EndOfStreamException"/> を投げます。
    /// </summary>
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
