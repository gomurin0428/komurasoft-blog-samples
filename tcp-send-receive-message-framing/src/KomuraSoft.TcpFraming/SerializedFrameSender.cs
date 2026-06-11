namespace KomuraSoft.TcpFraming;

/// <summary>
/// 1 つの接続に対する複数タスクからのフレーム書き込みを直列化します。
/// 並行 Write によるアプリケーションレベルの混線（A のヘッダーと B のヘッダーが
/// 交互に届くなど）を防ぎます。
/// </summary>
public sealed class SerializedFrameSender : IDisposable
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public async ValueTask SendFrameSafelyAsync(
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken);

        try
        {
            await LengthPrefixedProtocolWriter.WriteFrameAsync(
                stream,
                payload,
                cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public void Dispose()
    {
        _sendLock.Dispose();
    }
}
