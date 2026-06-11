namespace KomuraSoft.AdminBroker.Tests;

/// <summary>
/// 指定したチャンクサイズでしか読めないストリームです。
/// 「1 回の ReadAsync が要求サイズより少ないバイト数を返す」という状況を、
/// 実際のパイプを使わずに再現します（ReadExactAsync のループの検証用）。
/// </summary>
public sealed class ChunkedReadStream(byte[] data, int maxChunkSize) : Stream
{
    private int _position;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => data.Length;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int remaining = data.Length - _position;
        if (remaining == 0)
        {
            return 0;
        }

        int toCopy = Math.Min(Math.Min(count, maxChunkSize), remaining);
        Array.Copy(data, _position, buffer, offset, toCopy);
        _position += toCopy;
        return toCopy;
    }

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        int remaining = data.Length - _position;
        if (remaining == 0)
        {
            return ValueTask.FromResult(0);
        }

        int toCopy = Math.Min(Math.Min(buffer.Length, maxChunkSize), remaining);
        data.AsMemory(_position, toCopy).CopyTo(buffer);
        _position += toCopy;
        return ValueTask.FromResult(toCopy);
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
