using System.Buffers.Binary;
using System.Net;
using Xunit;

namespace KomuraSoft.OsiPacketAnatomy.Tests;

public class LayerParserTests
{
    [Fact]
    public void Ethernet_ParsesMacAddressesAndEtherType()
    {
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();

        EthernetFrame ethernet = EthernetFrame.Parse(frame);

        Assert.Equal("02:00:00:00:00:01", ethernet.DestinationMac);
        Assert.Equal("02:00:00:00:00:02", ethernet.SourceMac);
        Assert.True(ethernet.IsIpv4);
    }

    [Fact]
    public void Ipv4_ParsesAddressesTtlAndProtocol()
    {
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();
        EthernetFrame ethernet = EthernetFrame.Parse(frame);

        Ipv4Packet ip = Ipv4Packet.Parse(ethernet.Payload);

        Assert.Equal(SampleFrameBuilder.ClientAddress, ip.SourceAddress);
        Assert.Equal(SampleFrameBuilder.ServerAddress, ip.DestinationAddress);
        Assert.Equal(64, ip.TimeToLive);
        Assert.True(ip.IsTcp);
        Assert.True(ip.IsChecksumValid);
    }

    [Fact]
    public void Ipv4_CorruptedHeader_FailsChecksumValidation()
    {
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();
        frame[EthernetFrame.HeaderSize + 8] = 32; // TTL を書き換える

        EthernetFrame ethernet = EthernetFrame.Parse(frame);
        Ipv4Packet ip = Ipv4Packet.Parse(ethernet.Payload);

        Assert.False(ip.IsChecksumValid);
    }

    [Fact]
    public void Ipv4_EthernetPadding_IsExcludedFromPayload()
    {
        // Ethernet の最小フレーム長を満たすためのパディングが末尾に付いても、
        // IPv4 の TotalLength を信頼して上位層のデータを切り出せること
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();
        byte[] padded = new byte[frame.Length + 10];
        frame.CopyTo(padded, 0);

        EthernetFrame ethernet = EthernetFrame.Parse(padded);
        Ipv4Packet ip = Ipv4Packet.Parse(ethernet.Payload);

        TcpSegment tcp = TcpSegment.Parse(ip.Payload);
        Assert.Equal(
            SampleFrameBuilder.HttpRequest.Length,
            tcp.Payload.Length);
    }

    [Fact]
    public void Ipv4_FragmentFields_AreParsed()
    {
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();
        // MF=1、フラグメントオフセット=0x0010(8バイト単位) = 128バイト
        BinaryPrimitives.WriteUInt16BigEndian(
            frame.AsSpan(EthernetFrame.HeaderSize + 6, 2), 0x2010);

        EthernetFrame ethernet = EthernetFrame.Parse(frame);
        Ipv4Packet ip = Ipv4Packet.Parse(ethernet.Payload);

        Assert.True(ip.MoreFragments);
        Assert.Equal(128, ip.FragmentOffset);
        Assert.True(ip.IsNonInitialFragment);
    }

    [Fact]
    public void Ipv4_SampleFrame_IsNotFragmented()
    {
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();
        EthernetFrame ethernet = EthernetFrame.Parse(frame);

        Ipv4Packet ip = Ipv4Packet.Parse(ethernet.Payload);

        Assert.False(ip.MoreFragments);
        Assert.Equal(0, ip.FragmentOffset);
        Assert.False(ip.IsNonInitialFragment);
    }

    [Fact]
    public void Ipv6Header_IsRejected()
    {
        byte[] packet = new byte[40];
        packet[0] = 6 << 4;

        Assert.Throws<InvalidDataException>(() => Ipv4Packet.Parse(packet));
    }

    [Fact]
    public void Tcp_ParsesPortsAndFlags()
    {
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();
        EthernetFrame ethernet = EthernetFrame.Parse(frame);
        Ipv4Packet ip = Ipv4Packet.Parse(ethernet.Payload);

        TcpSegment tcp = TcpSegment.Parse(ip.Payload);

        Assert.Equal(SampleFrameBuilder.ClientPort, tcp.SourcePort);
        Assert.Equal(SampleFrameBuilder.ServerPort, tcp.DestinationPort);
        Assert.Equal(TcpFlags.Psh | TcpFlags.Ack, tcp.Flags);
    }

    [Fact]
    public void Tcp_ChecksumWithPseudoHeader_IsValid()
    {
        // 擬似ヘッダー(L3のアドレス情報)込みで再計算すると 0 になる =
        // SampleFrameBuilder が正しい TCP チェックサムを書き込んでいる
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();
        EthernetFrame ethernet = EthernetFrame.Parse(frame);
        Ipv4Packet ip = Ipv4Packet.Parse(ethernet.Payload);

        byte[] buffer = new byte[12 + ip.Payload.Length];
        ip.SourceAddress.GetAddressBytes().CopyTo(buffer, 0);
        ip.DestinationAddress.GetAddressBytes().CopyTo(buffer, 4);
        buffer[9] = Ipv4Packet.ProtocolTcp;
        BinaryPrimitives.WriteUInt16BigEndian(
            buffer.AsSpan(10, 2), (ushort)ip.Payload.Length);
        ip.Payload.Span.CopyTo(buffer.AsSpan(12));

        Assert.Equal(0, InternetChecksum.Compute(buffer));
    }

    [Fact]
    public void PcapWriter_ProducesFileWiresharkCanIdentify()
    {
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".pcap");

        try
        {
            PcapWriter.Write(path, [frame], DateTimeOffset.UnixEpoch);

            byte[] written = File.ReadAllBytes(path);

            // pcap のマジックナンバー(little-endian)と Ethernet リンクタイプ
            Assert.Equal(0xA1B2C3D4u, BinaryPrimitives.ReadUInt32LittleEndian(written));
            Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(written.AsSpan(20, 4)));

            // thiszone / sigfigs([8..16])が確実にゼロで書かれていること
            Assert.All(written[8..16], b => Assert.Equal(0, b));

            // グローバルヘッダー24 + レコードヘッダー16 + フレーム本体
            Assert.Equal(24 + 16 + frame.Length, written.Length);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
