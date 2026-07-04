using System.Net;
using System.Net.Sockets;
using System.Text;
using KomuraSoft.OsiPacketAnatomy;

// ============================================================
// Part 1: HTTP GET 1 回分の Ethernet フレームを解剖する
// ============================================================
Console.WriteLine("=== Part 1: 1つのフレームを7層に解剖する ===");
Console.WriteLine();

byte[] frame = SampleFrameBuilder.BuildHttpGetFrame();

Console.WriteLine($"フレーム全体({frame.Length} bytes)のヘックスダンプ:");
Console.WriteLine();
Console.WriteLine(HexDump.Render(frame));

IReadOnlyList<LayerInfo> layers = PacketDissector.Dissect(frame);

Console.WriteLine("外側(L2)から内側(L7)への入れ子:");
Console.WriteLine();
Console.WriteLine(PacketDissector.RenderNestedView(layers));

// Wireshark でそのまま開ける pcap ファイルとしても書き出す
string pcapPath = Path.Combine(AppContext.BaseDirectory, "sample-http-get.pcap");
PcapWriter.Write(pcapPath, [frame], DateTimeOffset.Now);
Console.WriteLine($"Wireshark で開ける pcap を書き出しました: {pcapPath}");
Console.WriteLine();

// ============================================================
// Part 2: あなたのコードはどの層を触っているのか
// ============================================================
// ループバックで実際に HTTP リクエストを 1 回送ります。
// アプリケーションが Socket.Send に渡すのは L7 のバイト列「だけ」で、
// Part 1 で見た TCP・IPv4・Ethernet ヘッダーは OS と NIC が付けています。
Console.WriteLine("=== Part 2: ループバックで実際に送ってみる ===");
Console.WriteLine();

using var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
int port = ((IPEndPoint)listener.LocalEndpoint).Port;

Task serverTask = Task.Run(async () =>
{
    using TcpClient client = await listener.AcceptTcpClientAsync();
    NetworkStream stream = client.GetStream();

    // 簡易サーバー: リクエストを読み捨てて 200 を返す
    byte[] buffer = new byte[4096];
    await stream.ReadAsync(buffer);

    byte[] response = Encoding.ASCII.GetBytes(
        "HTTP/1.1 200 OK\r\n" +
        "Content-Type: text/plain\r\n" +
        "Content-Length: 13\r\n" +
        "Connection: close\r\n" +
        "\r\n" +
        "Hello, OSI!\r\n");
    await stream.WriteAsync(response);
});

// L4 との境界: Socket を作った時点で「TCP で」「どのポートへ」までは指定するが、
// TCP ヘッダーそのものをアプリケーションが書くわけではない
using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
await socket.ConnectAsync(IPAddress.Loopback, port);

byte[] request = Encoding.ASCII.GetBytes(
    $"GET / HTTP/1.1\r\nHost: localhost:{port}\r\nConnection: close\r\n\r\n");

int sent = await socket.SendAsync(request);

Console.WriteLine($"Socket.SendAsync に渡したのは L7 のバイト列 {sent} bytes だけ:");
Console.WriteLine();
Console.WriteLine(HexDump.Render(request));
Console.WriteLine("TCP ヘッダー(L4)・IP ヘッダー(L3)は OS のプロトコルスタックが、");
Console.WriteLine("Ethernet ヘッダー(L2)と電気信号(L1)は NIC 側が付けています。");
Console.WriteLine();

// レスポンスを読み切る(Connection: close なので相手が閉じるまで)
var received = new MemoryStream();
byte[] recvBuffer = new byte[4096];
int n;
while ((n = await socket.ReceiveAsync(recvBuffer)) > 0)
{
    received.Write(recvBuffer, 0, n);
}

await serverTask;

Console.WriteLine("受信したレスポンス(これも L7 のバイト列だけが手元に届く):");
Console.WriteLine();
Console.WriteLine(Encoding.ASCII.GetString(received.ToArray()));
