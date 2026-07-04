namespace KomuraSoft.OsiPacketAnatomy;

/// <summary>
/// IPv4 ヘッダー・TCP で使われる「16bit ワードの 1 の補数和」チェックサム(RFC 1071)。
/// </summary>
public static class InternetChecksum
{
    /// <summary>
    /// チェックサムを計算します。
    /// チェックサムフィールドを 0 にした状態のデータに対して呼ぶと格納すべき値が、
    /// チェックサム込みの受信データに対して呼ぶと検証結果(正しければ 0)が得られます。
    /// </summary>
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        uint sum = 0;

        for (int i = 0; i + 1 < data.Length; i += 2)
        {
            sum += (uint)((data[i] << 8) | data[i + 1]);
        }

        if (data.Length % 2 == 1)
        {
            sum += (uint)(data[^1] << 8);
        }

        while ((sum >> 16) != 0)
        {
            sum = (sum & 0xFFFF) + (sum >> 16);
        }

        return (ushort)~sum;
    }
}
