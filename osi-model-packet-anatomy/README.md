# OSI参照モデルをパケット1個の解剖で理解する ── サンプルコード

ブログ記事「[OSI参照モデルをはっきりイメージする ── HTTPリクエスト1個を7層に解剖する](https://comcomponent.com/blog/osi-model-packet-anatomy/)」のサンプルコードです。

OSI参照モデルの7層は概念図の中だけの話ではなく、ネットワークを流れるフレーム1個のバイト列の中に **物理的に入れ子** になって存在します。このサンプルは、HTTP GET リクエスト1個を運ぶ Ethernet フレームを C# で組み立て(L7→L2)、外側から順に解剖して(L2→L7)、各層のヘッダーがフレームの何バイト目から何バイト目までを占めるかを表示します。

```
[ L2 Ethernet II | offset   0 |  14 bytes | dst=02:00:00:00:00:01 src=02:00:00:00:00:02 type=0x0800 (IPv4)
  [ L3 IPv4        | offset  14 |  20 bytes | 192.0.2.10 -> 198.51.100.80 TTL=64 proto=6 (TCP) checksum=OK
    [ L4 TCP         | offset  34 |  20 bytes | 52100 -> 80 [Psh, Ack] seq=1000 win=65535
      [ L7 HTTP        | offset  54 | 117 bytes | GET /index.html HTTP/1.1
```

組み立てたフレームは pcap ファイルとしても書き出すので、そのまま Wireshark で開いて同じ入れ子を確認できます。

## 構成

```
osi-model-packet-anatomy/
├── src/KomuraSoft.OsiPacketAnatomy/     解剖器の実装(クラスライブラリ)
│   ├── EthernetFrame.cs                 L2: Ethernet II ヘッダーの読み取り
│   ├── Ipv4Packet.cs                    L3: IPv4 ヘッダーの読み取り(チェックサム検証つき)
│   ├── TcpSegment.cs                    L4: TCP ヘッダーの読み取り
│   ├── PacketDissector.cs               L2→L7 に解剖して層ごとのバイト範囲を報告
│   ├── SampleFrameBuilder.cs            L7→L2 の順に「包んでいく」フレーム組み立て
│   ├── InternetChecksum.cs              RFC 1071 の 1 の補数和チェックサム
│   ├── PcapWriter.cs                    Wireshark で開ける pcap の書き出し
│   └── HexDump.cs                       Wireshark 風ヘックスダンプ
├── samples/Demo/                        解剖の実演 + ループバック TCP での実送信デモ
└── tests/KomuraSoft.OsiPacketAnatomy.Tests/  各層のパース・チェックサム・pcap 形式の検証
```

## 必要環境

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降

## 実行方法

デモ(フレームの解剖と、ループバック TCP での HTTP リクエスト実送信):

```console
dotnet run --project samples/Demo
```

実行すると `samples/Demo/bin/Debug/net8.0/sample-http-get.pcap` が生成されます。Wireshark で開くと、Ethernet → IPv4 → TCP → HTTP の入れ子をペイン上で確認できます。

テスト(各層のフィールド解釈、IPv4/TCP チェックサム、Ethernet パディングの除去、pcap 形式などを検証します):

```console
dotnet test
```

## ポイント

- 7層は「概念」ではなく、フレーム先頭からのバイト範囲として物理的に確認できる(各層の offset が前の層の直後から始まる)
- アプリケーションが `Socket.Send` に渡すのは L7 のバイト列だけで、TCP/IP ヘッダーは OS が、Ethernet ヘッダーは NIC 側が付ける
- IPv4 の上位層データは受信バッファーの長さではなく、ヘッダーの TotalLength を信頼して切り出す(Ethernet の最小フレーム長を満たすパディングがあるため)
- TCP チェックサムは L3 のアドレス情報を含む「擬似ヘッダー」込みで計算する ── 層がきれいに独立していない現実の一例
- HTTP かどうかはペイロードの中身で判断するしかない(ポート番号は慣習にすぎない)

詳しい解説は[記事本文](https://comcomponent.com/blog/osi-model-packet-anatomy/)をご覧ください。
