using System.Text;

namespace KomuraSoft.OsiPacketAnatomy;

/// <summary>
/// Wireshark 風の 16 バイト/行のヘックスダンプを生成します。
/// </summary>
public static class HexDump
{
    public static string Render(ReadOnlySpan<byte> data)
    {
        var sb = new StringBuilder();

        for (int offset = 0; offset < data.Length; offset += 16)
        {
            ReadOnlySpan<byte> line = data[offset..Math.Min(offset + 16, data.Length)];

            sb.Append($"{offset:x4}  ");

            for (int i = 0; i < 16; i++)
            {
                sb.Append(i < line.Length ? $"{line[i]:x2} " : "   ");
                if (i == 7) sb.Append(' ');
            }

            sb.Append(' ');
            foreach (byte b in line)
            {
                sb.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
