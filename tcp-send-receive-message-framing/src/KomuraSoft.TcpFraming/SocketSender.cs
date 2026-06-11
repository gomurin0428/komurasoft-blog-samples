using System.Net.Sockets;

namespace KomuraSoft.TcpFraming;

/// <summary>
/// <see cref="Socket.SendAsync(ReadOnlyMemory{byte}, SocketFlags, CancellationToken)"/> を
/// 直接使う場合に、要求したバイト数を送り切るまで繰り返すヘルパーです。
/// </summary>
public static class SocketSender
{
    public static async ValueTask SendAllAsync(
        Socket socket,
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken)
    {
        while (!buffer.IsEmpty)
        {
            int sent = await socket.SendAsync(
                buffer,
                SocketFlags.None,
                cancellationToken);

            if (sent == 0)
            {
                throw new IOException("Socket was closed while sending data.");
            }

            buffer = buffer[sent..];
        }
    }
}
