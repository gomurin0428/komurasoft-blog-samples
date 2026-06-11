# TCPでSendした単位ごとにReceiveできるという誤解 ── サンプルコード

ブログ記事「[TCPでSendした単位ごとにReceiveできるという誤解 ── バイトストリームとして扱うための受信設計](https://comcomponent.com/blog/2026/06/09/001-tcp-send-receive-message-framing/)」のサンプルコードです。

TCP はメッセージではなく、順序付きのバイトストリームを運びます。`Send` / `Write` した単位が受信側の `Receive` / `Read` 単位として保存される保証はないため、アプリケーション側でメッセージ境界（フレーミング）を設計する必要があります。

このサンプルでは、記事で紹介している**長さプレフィックス方式**（`[4バイトの本文長][本文]`）の実装と、その検証コードを提供します。

## 構成

```
tcp-send-receive-message-framing/
├── src/KomuraSoft.TcpFraming/           フレーミングの実装（クラスライブラリ）
│   ├── LengthPrefixedProtocol.cs        フレームの読み取り（記事 8 章）
│   ├── LengthPrefixedProtocolWriter.cs  フレームの書き込み（記事 9 章）
│   ├── SocketSender.cs                  Socket.Send の戻り値を見て送り切る（記事 10 章）
│   └── SerializedFrameSender.cs         並行 Write の直列化（記事 18 章）
├── samples/Demo/                        ループバック TCP での実演コンソールアプリ
└── tests/KomuraSoft.TcpFraming.Tests/   分割・結合・途中切断を再現するユニットテスト（記事 19 章）
```

## 必要環境

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降

## 実行方法

デモ（ループバック TCP 上でフレームを送受信します）:

```console
dotnet run --project samples/Demo
```

テスト（1 バイトずつの分割受信、複数フレームの結合、ヘッダー・本文の途中切断、巨大サイズ指定、UTF-8 マルチバイト文字の分割などを検証します）:

```console
dotnet test
```

## ポイント

- 受信は「何回 `Read` したか」ではなく「フレーム形式に従って何バイト読めたか」で判断する
- 必要なバイト数が決まっているなら、読み切るまでループする（1 回の `Read` で揃う保証はない）
- 本文長には必ず上限を設ける（`FF FF FF FF` のような不正な長さ指定への防御）
- フレーム境界での切断（正常終了）と、ヘッダー・本文の途中での切断（プロトコルエラー）を区別する
- 文字列は 1 メッセージ分のバイトが揃ってからデコードする（UTF-8 の途中分割に耐える）
- 同じ接続への複数タスクからの書き込みは直列化する

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/09/001-tcp-send-receive-message-framing/)をご覧ください。
