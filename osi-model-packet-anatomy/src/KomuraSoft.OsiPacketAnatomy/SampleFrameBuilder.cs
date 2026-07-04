using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace KomuraSoft.OsiPacketAnatomy;

/// <summary>
/// HTTP GET リクエスト 1 個を運ぶ Ethernet フレームを、L7 から L2 へ
/// 「包んでいく」順序どおりに組み立てます。
/// IPv4 ヘッダーチェックサムと TCP チェックサム(擬似ヘッダー込み)も
/// 実際に計算するので、Wireshark に読ませても正しいフレームになります。
/// </summary>
public static class SampleFrameBuilder
{
    // 例示用アドレス(RFC 5737 の TEST-NET を使用)
    public static readonly IPAddress ClientAddress = IPAddress.Parse("192.0.2.10");
    public static readonly IPAddress ServerAddress = IPAddress.Parse("198.51.100.80");
    public const ushort ClientPort = 52100;
    public const ushort ServerPort = 80;

    public const string HttpRequest =
        "GET /index.html HTTP/1.1\r\n" +
        "Host: example.com\r\n" +
        "User-Agent: KomuraSoftDemo/1.0\r\n" +
        "Accept: text/html\r\n" +
        "Connection: close\r\n" +
        "\r\n";

    /// <summary>
    /// [Ethernet [IPv4 [TCP [HTTP]]]] のフレームを構築します。
    /// </summary>
    public static byte[] BuildHttpGetFrame()
    {
        // L7: アプリケーションが Send に渡すのはこのバイト列「だけ」
        byte[] http = Encoding.ASCII.GetBytes(HttpRequest);

        // L4: OS のプロトコルスタックが TCP ヘッダーを前置する
        byte[] tcp = WrapInTcp(http);

        // L3: さらに IPv4 ヘッダーを前置する
        byte[] ip = WrapInIpv4(tcp);

        // L2: NIC ドライバーの手前で Ethernet ヘッダーを前置する
        return WrapInEthernet(ip);
    }

    private static byte[] WrapInTcp(byte[] payload)
    {
        byte[] segment = new byte[TcpSegment.MinHeaderSize + payload.Length];
        Span<byte> h = segment.AsSpan(0, TcpSegment.MinHeaderSize);

        BinaryPrimitives.WriteUInt16BigEndian(h[..2], ClientPort);
        BinaryPrimitives.WriteUInt16BigEndian(h[2..4], ServerPort);
        BinaryPrimitives.WriteUInt32BigEndian(h[4..8], 1000);       // seq(例示値)
        BinaryPrimitives.WriteUInt32BigEndian(h[8..12], 2000);      // ack(例示値)
        h[12] = 5 << 4;                                             // data offset = 5 words
        h[13] = (byte)(TcpFlags.Psh | TcpFlags.Ack);
        BinaryPrimitives.WriteUInt16BigEndian(h[14..16], 65535);    // window
        // h[16..18] checksum は後で計算、h[18..20] urgent pointer = 0

        payload.CopyTo(segment, TcpSegment.MinHeaderSize);

        ushort checksum = ComputeTcpChecksum(segment);
        BinaryPrimitives.WriteUInt16BigEndian(segment.AsSpan(16, 2), checksum);

        return segment;
    }

    /// <summary>
    /// TCP チェックサムは TCP セグメント単体ではなく、IPv4 の送信元・宛先
    /// アドレスなどを並べた「擬似ヘッダー」を先頭に付けて計算します。
    /// L4 のチェックサムが L3 の情報に依存する、層がきれいに独立していない
    /// 現実の一例です。
    /// </summary>
    private static ushort ComputeTcpChecksum(byte[] segment)
    {
        byte[] buffer = new byte[12 + segment.Length];
        Span<byte> pseudo = buffer.AsSpan(0, 12);

        ClientAddress.GetAddressBytes().CopyTo(pseudo[..4]);
        ServerAddress.GetAddressBytes().CopyTo(pseudo[4..8]);
        pseudo[9] = Ipv4Packet.ProtocolTcp;
        BinaryPrimitives.WriteUInt16BigEndian(pseudo[10..12], (ushort)segment.Length);

        segment.CopyTo(buffer, 12);

        return InternetChecksum.Compute(buffer);
    }

    private static byte[] WrapInIpv4(byte[] payload)
    {
        byte[] packet = new byte[Ipv4Packet.MinHeaderSize + payload.Length];
        Span<byte> h = packet.AsSpan(0, Ipv4Packet.MinHeaderSize);

        h[0] = (4 << 4) | 5;                                        // version=4, IHL=5 words
        BinaryPrimitives.WriteUInt16BigEndian(h[2..4], (ushort)packet.Length);
        BinaryPrimitives.WriteUInt16BigEndian(h[4..6], 0x1234);     // identification(例示値)
        h[6] = 0x40;                                                // flags: Don't Fragment
        h[8] = 64;                                                  // TTL
        h[9] = Ipv4Packet.ProtocolTcp;
        ClientAddress.GetAddressBytes().CopyTo(h[12..16]);
        ServerAddress.GetAddressBytes().CopyTo(h[16..20]);

        ushort checksum = InternetChecksum.Compute(h);
        BinaryPrimitives.WriteUInt16BigEndian(h[10..12], checksum);

        payload.CopyTo(packet, Ipv4Packet.MinHeaderSize);

        return packet;
    }

    private static byte[] WrapInEthernet(byte[] payload)
    {
        byte[] frame = new byte[EthernetFrame.HeaderSize + payload.Length];
        Span<byte> h = frame.AsSpan(0, EthernetFrame.HeaderSize);

        // ローカル管理アドレス(0x02 始まり)を例示用に使用
        ReadOnlySpan<byte> dstMac = [0x02, 0x00, 0x00, 0x00, 0x00, 0x01];
        ReadOnlySpan<byte> srcMac = [0x02, 0x00, 0x00, 0x00, 0x00, 0x02];
        dstMac.CopyTo(h[..6]);
        srcMac.CopyTo(h[6..12]);
        BinaryPrimitives.WriteUInt16BigEndian(h[12..14], EthernetFrame.EtherTypeIpv4);

        payload.CopyTo(frame, EthernetFrame.HeaderSize);

        return frame;
    }
}
