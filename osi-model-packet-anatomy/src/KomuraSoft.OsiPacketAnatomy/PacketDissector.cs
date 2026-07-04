using System.Text;

namespace KomuraSoft.OsiPacketAnatomy;

/// <summary>
/// 解剖された 1 層分の情報。
/// フレーム先頭からのバイト位置(Offset / Length)を持つのがポイントで、
/// 「7 層が同じバイト列の中に物理的に入れ子になっている」ことを示せます。
/// </summary>
public sealed record LayerInfo(
    int OsiLayer,
    string OsiLayerName,
    string ProtocolName,
    int Offset,
    int Length,
    string Summary);

/// <summary>
/// Ethernet フレーム 1 個を外側(L2)から内側(L7)へ順に解剖します。
/// </summary>
public static class PacketDissector
{
    /// <summary>
    /// フレームを解剖し、外側の層から順に並んだ層情報を返します。
    /// IPv4/TCP 以外(ARP など)は、その層をデータとして報告した時点で止まります。
    /// </summary>
    public static IReadOnlyList<LayerInfo> Dissect(ReadOnlyMemory<byte> frame)
    {
        var layers = new List<LayerInfo>();
        int offset = 0;

        // --- L2: Ethernet ---
        EthernetFrame ethernet = EthernetFrame.Parse(frame);
        layers.Add(new LayerInfo(
            OsiLayer: 2,
            OsiLayerName: "データリンク層",
            ProtocolName: "Ethernet II",
            Offset: offset,
            Length: EthernetFrame.HeaderSize,
            Summary: $"dst={ethernet.DestinationMac} src={ethernet.SourceMac} " +
                     $"type=0x{ethernet.EtherType:x4}{(ethernet.IsIpv4 ? " (IPv4)" : "")}"));
        offset += EthernetFrame.HeaderSize;

        if (!ethernet.IsIpv4)
        {
            layers.Add(OpaquePayload(offset, ethernet.Payload.Length, "Ethernet"));
            return layers;
        }

        // --- L3: IPv4 ---
        Ipv4Packet ip = Ipv4Packet.Parse(ethernet.Payload);
        layers.Add(new LayerInfo(
            OsiLayer: 3,
            OsiLayerName: "ネットワーク層",
            ProtocolName: "IPv4",
            Offset: offset,
            Length: ip.HeaderLength,
            Summary: $"{ip.SourceAddress} -> {ip.DestinationAddress} " +
                     $"TTL={ip.TimeToLive} proto={ip.Protocol}{(ip.IsTcp ? " (TCP)" : "")} " +
                     $"checksum={(ip.IsChecksumValid ? "OK" : "NG")}"));
        offset += ip.HeaderLength;

        if (!ip.IsTcp)
        {
            layers.Add(OpaquePayload(offset, ip.Payload.Length, "IPv4"));
            return layers;
        }

        // --- L4: TCP ---
        TcpSegment tcp = TcpSegment.Parse(ip.Payload);
        layers.Add(new LayerInfo(
            OsiLayer: 4,
            OsiLayerName: "トランスポート層",
            ProtocolName: "TCP",
            Offset: offset,
            Length: tcp.HeaderLength,
            Summary: $"{tcp.SourcePort} -> {tcp.DestinationPort} " +
                     $"[{tcp.Flags}] seq={tcp.SequenceNumber} win={tcp.WindowSize}"));
        offset += tcp.HeaderLength;

        if (tcp.Payload.IsEmpty)
        {
            return layers;
        }

        // --- L7: アプリケーションデータ ---
        // ここから先は TCP にとってはただのバイト列。HTTP かどうかは
        // 中身を見て初めて分かる(ポート番号は慣習にすぎない)。
        bool looksLikeHttp = LooksLikeHttp(tcp.Payload.Span);
        layers.Add(new LayerInfo(
            OsiLayer: 7,
            OsiLayerName: "アプリケーション層",
            ProtocolName: looksLikeHttp ? "HTTP" : "Data",
            Offset: offset,
            Length: tcp.Payload.Length,
            Summary: looksLikeHttp
                ? FirstLine(tcp.Payload.Span)
                : $"{tcp.Payload.Length} bytes of application data"));

        return layers;
    }

    /// <summary>
    /// 解剖結果を「入れ子」の形で描画します。
    /// </summary>
    public static string RenderNestedView(IReadOnlyList<LayerInfo> layers)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < layers.Count; i++)
        {
            LayerInfo layer = layers[i];
            string indent = new(' ', i * 2);
            sb.AppendLine(
                $"{indent}[ L{layer.OsiLayer} {layer.ProtocolName,-11} | " +
                $"offset {layer.Offset,3} | {layer.Length,3} bytes | {layer.Summary}");
        }
        return sb.ToString();
    }

    private static LayerInfo OpaquePayload(int offset, int length, string outerProtocol)
        => new(
            OsiLayer: 0,
            OsiLayerName: "(未対応)",
            ProtocolName: "Data",
            Offset: offset,
            Length: length,
            Summary: $"payload of {outerProtocol} (not dissected by this sample)");

    private static bool LooksLikeHttp(ReadOnlySpan<byte> payload)
    {
        // リクエスト行(メソッド)またはステータス行(HTTP/)で始まるかの簡易判定
        string[] prefixes = ["GET ", "POST ", "PUT ", "DELETE ", "HEAD ", "HTTP/"];
        foreach (string prefix in prefixes)
        {
            if (payload.Length >= prefix.Length &&
                Encoding.ASCII.GetString(payload[..prefix.Length]) == prefix)
            {
                return true;
            }
        }
        return false;
    }

    private static string FirstLine(ReadOnlySpan<byte> payload)
    {
        int end = payload.IndexOf((byte)'\r');
        return Encoding.ASCII.GetString(end >= 0 ? payload[..end] : payload);
    }
}
