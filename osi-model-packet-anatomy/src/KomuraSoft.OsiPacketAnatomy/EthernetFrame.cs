using System.Buffers.Binary;

namespace KomuraSoft.OsiPacketAnatomy;

/// <summary>
/// Ethernet II フレームのヘッダー(OSI L2:データリンク層)を読み取ります。
/// [宛先MAC 6][送信元MAC 6][EtherType 2][ペイロード]という構造です。
/// </summary>
public sealed class EthernetFrame
{
    public const int HeaderSize = 14;

    /// <summary>IPv4 を示す EtherType。</summary>
    public const ushort EtherTypeIpv4 = 0x0800;

    /// <summary>ARP を示す EtherType。</summary>
    public const ushort EtherTypeArp = 0x0806;

    public required string DestinationMac { get; init; }
    public required string SourceMac { get; init; }
    public required ushort EtherType { get; init; }

    /// <summary>EtherType の次のバイトから始まる上位層(L3)のデータ。</summary>
    public required ReadOnlyMemory<byte> Payload { get; init; }

    public bool IsIpv4 => EtherType == EtherTypeIpv4;

    /// <summary>
    /// 生のフレームバイト列から Ethernet ヘッダーを読み取ります。
    /// </summary>
    public static EthernetFrame Parse(ReadOnlyMemory<byte> raw)
    {
        if (raw.Length < HeaderSize)
        {
            throw new InvalidDataException(
                $"Ethernet frame is truncated: {raw.Length} bytes (need at least {HeaderSize}).");
        }

        ReadOnlySpan<byte> span = raw.Span;

        return new EthernetFrame
        {
            DestinationMac = FormatMac(span[..6]),
            SourceMac = FormatMac(span[6..12]),
            EtherType = BinaryPrimitives.ReadUInt16BigEndian(span[12..14]),
            Payload = raw[HeaderSize..],
        };
    }

    private static string FormatMac(ReadOnlySpan<byte> mac)
        => string.Join(":", mac.ToArray().Select(b => b.ToString("x2")));
}
