using System.Buffers.Binary;

namespace KomuraSoft.OsiPacketAnatomy;

/// <summary>
/// Ethernet フレームを pcap 形式で書き出します。
/// 書き出したファイルはそのまま Wireshark で開けます。
/// </summary>
public static class PcapWriter
{
    private const uint MagicNumber = 0xA1B2C3D4;
    private const ushort VersionMajor = 2;
    private const ushort VersionMinor = 4;
    private const uint SnapLength = 65535;
    private const uint LinkTypeEthernet = 1;

    public static void Write(string path, IEnumerable<byte[]> frames, DateTimeOffset timestamp)
    {
        using var stream = File.Create(path);

        Span<byte> global = stackalloc byte[24];
        BinaryPrimitives.WriteUInt32LittleEndian(global[..4], MagicNumber);
        BinaryPrimitives.WriteUInt16LittleEndian(global[4..6], VersionMajor);
        BinaryPrimitives.WriteUInt16LittleEndian(global[6..8], VersionMinor);
        // [8..16] thiszone / sigfigs = 0
        BinaryPrimitives.WriteUInt32LittleEndian(global[16..20], SnapLength);
        BinaryPrimitives.WriteUInt32LittleEndian(global[20..24], LinkTypeEthernet);
        stream.Write(global);

        Span<byte> record = stackalloc byte[16];
        foreach (byte[] frame in frames)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(record[..4], (uint)timestamp.ToUnixTimeSeconds());
            BinaryPrimitives.WriteUInt32LittleEndian(record[4..8], (uint)(timestamp.Millisecond * 1000));
            BinaryPrimitives.WriteUInt32LittleEndian(record[8..12], (uint)frame.Length);
            BinaryPrimitives.WriteUInt32LittleEndian(record[12..16], (uint)frame.Length);
            stream.Write(record);
            stream.Write(frame);
        }
    }
}
