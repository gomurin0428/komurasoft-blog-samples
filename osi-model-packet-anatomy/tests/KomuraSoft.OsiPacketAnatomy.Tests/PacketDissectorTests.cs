using System.Buffers.Binary;
using System.Text;
using Xunit;

namespace KomuraSoft.OsiPacketAnatomy.Tests;

public class PacketDissectorTests
{
    [Fact]
    public void SampleFrame_DissectsIntoFourLayers()
    {
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();

        IReadOnlyList<LayerInfo> layers = PacketDissector.Dissect(frame);

        Assert.Equal(4, layers.Count);
        Assert.Equal(new[] { "Ethernet II", "IPv4", "TCP", "HTTP" },
            layers.Select(l => l.ProtocolName).ToArray());
        Assert.Equal(new[] { 2, 3, 4, 7 }, layers.Select(l => l.OsiLayer).ToArray());
    }

    [Fact]
    public void SampleFrame_LayersAreContiguousAndCoverWholeFrame()
    {
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();

        IReadOnlyList<LayerInfo> layers = PacketDissector.Dissect(frame);

        // 各層は前の層の直後から始まる(=物理的に入れ子になっている)
        int expectedOffset = 0;
        foreach (LayerInfo layer in layers)
        {
            Assert.Equal(expectedOffset, layer.Offset);
            expectedOffset += layer.Length;
        }

        // ヘッダー+ペイロードでフレーム全体をちょうど使い切る
        Assert.Equal(frame.Length, expectedOffset);
    }

    [Fact]
    public void SampleFrame_HeaderSizesMatchWellKnownValues()
    {
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();

        IReadOnlyList<LayerInfo> layers = PacketDissector.Dissect(frame);

        Assert.Equal(14, layers[0].Length); // Ethernet II ヘッダー
        Assert.Equal(20, layers[1].Length); // IPv4 ヘッダー(オプションなし)
        Assert.Equal(20, layers[2].Length); // TCP ヘッダー(オプションなし)
        Assert.Equal(
            Encoding.ASCII.GetByteCount(SampleFrameBuilder.HttpRequest),
            layers[3].Length);
    }

    [Fact]
    public void SampleFrame_HttpSummaryShowsRequestLine()
    {
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();

        IReadOnlyList<LayerInfo> layers = PacketDissector.Dissect(frame);

        Assert.Equal("GET /index.html HTTP/1.1", layers[3].Summary);
    }

    [Fact]
    public void ArpFrame_StopsAfterEthernetLayer()
    {
        // EtherType = ARP のフレーム。この解剖器は L3 として扱えないので
        // 「L2 + 不透明なデータ」として報告して止まる。
        byte[] frame = new byte[EthernetFrame.HeaderSize + 28];
        BinaryPrimitives.WriteUInt16BigEndian(
            frame.AsSpan(12, 2), EthernetFrame.EtherTypeArp);

        IReadOnlyList<LayerInfo> layers = PacketDissector.Dissect(frame);

        Assert.Equal(2, layers.Count);
        Assert.Equal("Ethernet II", layers[0].ProtocolName);
        Assert.Equal("Data", layers[1].ProtocolName);
    }

    [Fact]
    public void TruncatedFrame_ThrowsInvalidData()
    {
        byte[] tooShort = new byte[10];

        Assert.Throws<InvalidDataException>(
            () => PacketDissector.Dissect(tooShort));
    }

    [Fact]
    public void NestedView_IndentsInnerLayers()
    {
        byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();
        IReadOnlyList<LayerInfo> layers = PacketDissector.Dissect(frame);

        string view = PacketDissector.RenderNestedView(layers);
        string[] lines = view.TrimEnd().Split(Environment.NewLine);

        Assert.Equal(4, lines.Length);
        Assert.StartsWith("[ L2", lines[0]);
        Assert.StartsWith("  [ L3", lines[1]);
        Assert.StartsWith("    [ L4", lines[2]);
        Assert.StartsWith("      [ L7", lines[3]);
    }
}
