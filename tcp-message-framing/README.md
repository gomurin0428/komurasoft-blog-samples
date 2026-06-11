# TCPメッセージフレーミング（長さプレフィックス方式）サンプル

ブログ記事 [TCPでSendした単位ごとにReceiveできるという誤解 ── バイトストリームとして扱うための受信設計](https://comcomponent.com/blog/2026/06/09/001-tcp-send-receive-message-framing/) のサンプルコードです。

TCPはバイトストリームであり、`Send` した単位がそのまま `Receive` で届く保証はありません。
このサンプルでは、`[4バイトの本文長][本文]` という長さプレフィックス方式でフレーム境界を自分で定義し、
分割・結合されたバイト列から1メッセージずつ確実に復元する受信設計を実装しています。

## 構成

| ファイル | 内容 |
| --- | --- |
| `LengthPrefixedProtocol.cs` | フレームの読み取り（`ReadFrameAsync`）。ヘッダー・本文が揃うまで読み、切断やプロトコルエラーを区別して扱う |
| `LengthPrefixedProtocolWriter.cs` | フレームの書き込み（`WriteFrameAsync`） |
| `FrameSender.cs` | 複数タスクからの送信をフレーム単位で直列化する `SemaphoreSlim` ベースのラッパー |
| `Program.cs` | ループバック上のサーバー／クライアントで動作確認するデモ |

## 実行方法

.NET 8 SDK が必要です。

```bash
cd tcp-message-framing
dotnet run
```

デモでは、クライアントが3つのJSONメッセージをフレーム化したあと、
**フレーム境界を無視した7バイト刻み**で送信します。
それでも受信側は長さプレフィックスを頼りに、ちょうど3つのメッセージを復元できます。

```text
[client] sent 7 bytes (frame境界とは無関係)
...
[server] received 1 frame: {"command":"login","user":"komura"}
[server] received 1 frame: {"command":"get","target":"item-001"}
[server] received 1 frame: {"command":"quit"}
done.
```

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/09/001-tcp-send-receive-message-framing/)を参照してください。
