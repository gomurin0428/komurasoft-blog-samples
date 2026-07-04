using System.Buffers.Binary;

namespace KomuraSoft.OsiPacketAnatomy;

/// <summary>
/// TCP セグメントのヘッダー(OSI L4:トランスポート層)を読み取ります。
/// </summary>
public sealed class TcpSegment
{
    public const int MinHeaderSize = 20;

    public required ushort SourcePort { get; init; }
    public required ushort DestinationPort { get; init; }
    public required uint SequenceNumber { get; init; }
    public required uint AcknowledgmentNumber { get; init; }
    public required int HeaderLength { get; init; }
    public required TcpFlags Flags { get; init; }
    public required ushort WindowSize { get; init; }

    /// <summary>ヘッダー(+オプション)の直後から始まるアプリケーションデータ(L7)。</summary>
    public required ReadOnlyMemory<byte> Payload { get; init; }

    /// <summary>
    /// IPv4 ペイロードから TCP ヘッダーを読み取ります。
    /// </summary>
    public static TcpSegment Parse(ReadOnlyMemory<byte> raw)
    {
        if (raw.Length < MinHeaderSize)
        {
            throw new InvalidDataException(
                $"TCP segment is truncated: {raw.Length} bytes (need at least {MinHeaderSize}).");
        }

        ReadOnlySpan<byte> span = raw.Span;

        // Data Offset(上位4ビット)は 32bit ワード単位のヘッダー長
        int headerLength = (span[12] >> 4) * 4;
        if (headerLength < MinHeaderSize || headerLength > raw.Length)
        {
            throw new InvalidDataException($"Invalid TCP header length: {headerLength} bytes.");
        }

        return new TcpSegment
        {
            SourcePort = BinaryPrimitives.ReadUInt16BigEndian(span[..2]),
            DestinationPort = BinaryPrimitives.ReadUInt16BigEndian(span[2..4]),
            SequenceNumber = BinaryPrimitives.ReadUInt32BigEndian(span[4..8]),
            AcknowledgmentNumber = BinaryPrimitives.ReadUInt32BigEndian(span[8..12]),
            HeaderLength = headerLength,
            Flags = (TcpFlags)(span[13] & 0x3F),
            WindowSize = BinaryPrimitives.ReadUInt16BigEndian(span[14..16]),
            Payload = raw[headerLength..],
        };
    }
}

/// <summary>TCP ヘッダーの制御フラグ。</summary>
[Flags]
public enum TcpFlags : byte
{
    None = 0,
    Fin = 1 << 0,
    Syn = 1 << 1,
    Rst = 1 << 2,
    Psh = 1 << 3,
    Ack = 1 << 4,
    Urg = 1 << 5,
}
