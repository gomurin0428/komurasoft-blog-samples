using System.Buffers.Binary;
using System.Net;

namespace KomuraSoft.OsiPacketAnatomy;

/// <summary>
/// IPv4 パケットのヘッダー(OSI L3:ネットワーク層)を読み取ります。
/// </summary>
public sealed class Ipv4Packet
{
    public const int MinHeaderSize = 20;

    /// <summary>TCP を示すプロトコル番号。</summary>
    public const byte ProtocolTcp = 6;

    /// <summary>UDP を示すプロトコル番号。</summary>
    public const byte ProtocolUdp = 17;

    public required int Version { get; init; }
    public required int HeaderLength { get; init; }
    public required int TotalLength { get; init; }

    /// <summary>More Fragments フラグ。後続フラグメントが存在するか。</summary>
    public required bool MoreFragments { get; init; }

    /// <summary>フラグメントオフセット(バイト単位。ヘッダー上は8バイト単位で格納)。</summary>
    public required int FragmentOffset { get; init; }

    public required byte TimeToLive { get; init; }
    public required byte Protocol { get; init; }
    public required ushort HeaderChecksum { get; init; }
    public required IPAddress SourceAddress { get; init; }
    public required IPAddress DestinationAddress { get; init; }

    /// <summary>ヘッダーの直後から TotalLength までの上位層(L4)のデータ。</summary>
    public required ReadOnlyMemory<byte> Payload { get; init; }

    /// <summary>受信時に計算し直したチェックサムがヘッダーの値と一致したか。</summary>
    public required bool IsChecksumValid { get; init; }

    public bool IsTcp => Protocol == ProtocolTcp;

    /// <summary>
    /// 分割されたパケットの2個目以降のフラグメントか。
    /// この場合、ペイロードは上位層ヘッダーの続きではなくデータの断片なので、
    /// TCP などとして解釈してはいけない。
    /// </summary>
    public bool IsNonInitialFragment => FragmentOffset > 0;

    /// <summary>
    /// Ethernet ペイロードから IPv4 ヘッダーを読み取ります。
    /// Ethernet の最小フレーム長(60バイト)を満たすためのパディングが
    /// 末尾に付いていることがあるため、上位層に渡すデータは
    /// ヘッダーの TotalLength を信頼して切り出します。
    /// </summary>
    public static Ipv4Packet Parse(ReadOnlyMemory<byte> raw)
    {
        if (raw.Length < MinHeaderSize)
        {
            throw new InvalidDataException(
                $"IPv4 packet is truncated: {raw.Length} bytes (need at least {MinHeaderSize}).");
        }

        ReadOnlySpan<byte> span = raw.Span;

        int version = span[0] >> 4;
        if (version != 4)
        {
            throw new InvalidDataException($"Not an IPv4 packet: version={version}.");
        }

        int headerLength = (span[0] & 0x0F) * 4;
        if (headerLength < MinHeaderSize || headerLength > raw.Length)
        {
            throw new InvalidDataException($"Invalid IPv4 header length: {headerLength} bytes.");
        }

        int totalLength = BinaryPrimitives.ReadUInt16BigEndian(span[2..4]);
        if (totalLength < headerLength || totalLength > raw.Length)
        {
            throw new InvalidDataException(
                $"Invalid IPv4 total length: {totalLength} bytes (buffer is {raw.Length}).");
        }

        ushort flagsAndOffset = BinaryPrimitives.ReadUInt16BigEndian(span[6..8]);

        return new Ipv4Packet
        {
            Version = version,
            HeaderLength = headerLength,
            TotalLength = totalLength,
            MoreFragments = (flagsAndOffset & 0x2000) != 0,
            FragmentOffset = (flagsAndOffset & 0x1FFF) * 8,
            TimeToLive = span[8],
            Protocol = span[9],
            HeaderChecksum = BinaryPrimitives.ReadUInt16BigEndian(span[10..12]),
            SourceAddress = new IPAddress(span[12..16]),
            DestinationAddress = new IPAddress(span[16..20]),
            Payload = raw[headerLength..totalLength],
            IsChecksumValid = InternetChecksum.Compute(span[..headerLength]) == 0,
        };
    }
}
