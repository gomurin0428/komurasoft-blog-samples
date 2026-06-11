namespace TcpMessageFraming;

/// <summary>
/// 複数のスレッド／タスクから同じ接続へ送信するとき、
/// フレーム単位の書き込みが交錯しないように直列化する。
/// </summary>
public sealed class FrameSender
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
}
