using System.Net;
using System.Net.Sockets;
using System.Text;
using TcpMessageFraming;

// ループバック上でサーバーとクライアントを立ち上げ、
// 「送信側がわざと細切れにSendしても、受信側はフレーム単位で復元できる」ことを確認するデモ。

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
CancellationToken cancellationToken = cts.Token;

var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
int port = ((IPEndPoint)listener.LocalEndpoint).Port;

Task serverTask = RunServerAsync(listener, cancellationToken);

using (var client = new TcpClient())
{
    await client.ConnectAsync(IPAddress.Loopback, port, cancellationToken);
    using NetworkStream stream = client.GetStream();

    string[] messages =
    [
        """{"command":"login","user":"komura"}""",
        """{"command":"get","target":"item-001"}""",
        """{"command":"quit"}""",
    ];

    // まず3つのメッセージをフレーム化して、1本のバイト列にまとめる
    var combined = new MemoryStream();
    foreach (string message in messages)
    {
        byte[] payload = Encoding.UTF8.GetBytes(message);
        await LengthPrefixedProtocolWriter.WriteFrameAsync(
            combined,
            payload,
            cancellationToken);
    }

    // TCPの分割・結合を模擬するため、フレーム境界を無視した7バイト刻みで送信する
    byte[] bytes = combined.ToArray();
    for (int offset = 0; offset < bytes.Length; offset += 7)
    {
        int count = Math.Min(7, bytes.Length - offset);
        await stream.WriteAsync(bytes.AsMemory(offset, count), cancellationToken);
        await stream.FlushAsync(cancellationToken);
        Console.WriteLine($"[client] sent {count} bytes (frame境界とは無関係)");
    }
}

await serverTask;
Console.WriteLine("done.");

static async Task RunServerAsync(TcpListener listener, CancellationToken cancellationToken)
{
    using TcpClient connection = await listener.AcceptTcpClientAsync(cancellationToken);
    listener.Stop();

    using NetworkStream stream = connection.GetStream();

    while (true)
    {
        byte[]? payload = await LengthPrefixedProtocol.ReadFrameAsync(
            stream,
            cancellationToken);

        if (payload is null)
        {
            // 相手がフレーム境界できれいに切断した
            break;
        }

        string message = Encoding.UTF8.GetString(payload);
        Console.WriteLine($"[server] received 1 frame: {message}");
    }
}
