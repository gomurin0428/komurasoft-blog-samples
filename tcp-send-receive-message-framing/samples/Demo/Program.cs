using System.Net;
using System.Net.Sockets;
using System.Text;
using KomuraSoft.TcpFraming;

// ループバック TCP 上で長さプレフィックス方式のフレーミングを実演するデモです。
// クライアントは複数のフレーム（日本語を含む UTF-8 の JSON）を連続して送信し、
// サーバーは ReadAsync が何バイトずつ返しても 1 フレームずつ正しく切り出します。

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
CancellationToken cancellationToken = cts.Token;

var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
int port = ((IPEndPoint)listener.LocalEndpoint).Port;
Console.WriteLine($"[server] listening on 127.0.0.1:{port}");

Task serverTask = RunServerAsync(listener, cancellationToken);
await RunClientAsync(port, cancellationToken);
await serverTask;

listener.Stop();
Console.WriteLine("[demo] done");

static async Task RunServerAsync(TcpListener listener, CancellationToken cancellationToken)
{
    using TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);
    await using NetworkStream stream = client.GetStream();

    while (true)
    {
        byte[]? payload = await LengthPrefixedProtocol.ReadFrameAsync(
            stream,
            cancellationToken);

        if (payload is null)
        {
            // 相手がフレーム境界できれいに切断した
            Console.WriteLine("[server] peer closed the connection at a frame boundary");
            break;
        }

        // フレーム本文をバイトとして読み切ってから文字列に戻す。
        // この順番なら、UTF-8 文字の途中で Read が分割されても問題にならない。
        string json = Encoding.UTF8.GetString(payload);
        Console.WriteLine($"[server] received frame ({payload.Length} bytes): {json}");
    }
}

static async Task RunClientAsync(int port, CancellationToken cancellationToken)
{
    using var client = new TcpClient();
    await client.ConnectAsync(IPAddress.Loopback, port, cancellationToken);
    await using NetworkStream stream = client.GetStream();

    using var sender = new SerializedFrameSender();

    string[] messages =
    [
        """{"command":"login","user":"komura"}""",
        """{"command":"get","item":"item-001"}""",
        """{"message":"こんにちは"}""",
        """{"command":"quit"}""",
    ];

    foreach (string message in messages)
    {
        // 送信側は、必ずエンコード後のバイト配列を基準に長さを計算する
        byte[] payload = Encoding.UTF8.GetBytes(message);
        await sender.SendFrameSafelyAsync(stream, payload, cancellationToken);
        Console.WriteLine($"[client] sent frame ({payload.Length} bytes): {message}");
    }

    // フレーム境界で切断する（サーバー側では正常終了として観測される）
    client.Client.Shutdown(SocketShutdown.Send);

    // サーバーが読み終わるまで少し待つ
    await Task.Delay(200, cancellationToken);
}
