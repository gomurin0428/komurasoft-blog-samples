using System.Buffers.Binary;
using System.Text;
using Xunit;

namespace KomuraSoft.TcpFraming.Tests;

public class LengthPrefixedProtocolTests
{
    private static byte[] BuildFrame(byte[] payload)
    {
        byte[] frame = new byte[4 + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(frame, payload.Length);
        payload.CopyTo(frame, 4);
        return frame;
    }

    private static byte[] BuildFrame(string payloadText)
        => BuildFrame(Encoding.UTF8.GetBytes(payloadText));

    [Theory]
    [InlineData(1)]   // 1 バイトずつ届く（ヘッダーも本文も分割される）
    [InlineData(3)]   // ヘッダーが 4 バイト境界以外で分割される
    [InlineData(4096)]
    public async Task ReadFrameAsync_ReassemblesFrame_RegardlessOfChunkSize(int chunkSize)
    {
        byte[] frame = BuildFrame("""{"command":"login","user":"komura"}""");
        using var stream = new ChunkedReadStream(frame, chunkSize);

        byte[]? payload = await LengthPrefixedProtocol.ReadFrameAsync(
            stream,
            CancellationToken.None);

        Assert.NotNull(payload);
        Assert.Equal(
            """{"command":"login","user":"komura"}""",
            Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ReadFrameAsync_SplitsCombinedFrames()
    {
        // 複数フレームが 1 回の受信バッファに入っている状況
        byte[] data = BuildFrame("first").Concat(BuildFrame("second")).ToArray();
        using var stream = new ChunkedReadStream(data, maxChunkSize: 4096);

        byte[]? first = await LengthPrefixedProtocol.ReadFrameAsync(stream, CancellationToken.None);
        byte[]? second = await LengthPrefixedProtocol.ReadFrameAsync(stream, CancellationToken.None);
        byte[]? end = await LengthPrefixedProtocol.ReadFrameAsync(stream, CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal("first", Encoding.UTF8.GetString(first));
        Assert.Equal("second", Encoding.UTF8.GetString(second));
        Assert.Null(end); // フレーム境界での終了は正常終了
    }

    [Fact]
    public async Task ReadFrameAsync_SurvivesUtf8SplitMidCharacter()
    {
        // 「こんにちは」は 5 文字だが UTF-8 では 15 バイト。
        // 1 バイトずつ読んでもマルチバイト文字が壊れないことを確認する。
        byte[] frame = BuildFrame("""{"message":"こんにちは"}""");
        using var stream = new ChunkedReadStream(frame, maxChunkSize: 1);

        byte[]? payload = await LengthPrefixedProtocol.ReadFrameAsync(
            stream,
            CancellationToken.None);

        Assert.NotNull(payload);
        Assert.Equal(
            """{"message":"こんにちは"}""",
            Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public async Task ReadFrameAsync_ReturnsNull_WhenPeerClosesAtFrameBoundary()
    {
        using var stream = new ChunkedReadStream([], maxChunkSize: 4096);

        byte[]? payload = await LengthPrefixedProtocol.ReadFrameAsync(
            stream,
            CancellationToken.None);

        Assert.Null(payload);
    }

    [Fact]
    public async Task ReadFrameAsync_Throws_WhenHeaderIsTruncated()
    {
        // 4 バイトヘッダーのうち 2 バイトだけ届いて終了する
        byte[] data = [0x00, 0x00];
        using var stream = new ChunkedReadStream(data, maxChunkSize: 4096);

        await Assert.ThrowsAsync<EndOfStreamException>(async () =>
            await LengthPrefixedProtocol.ReadFrameAsync(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadFrameAsync_Throws_WhenPayloadIsTruncated()
    {
        // 本文長 100 のうち 60 バイトだけ届いて終了する
        byte[] frame = BuildFrame(new byte[100]);
        byte[] truncated = frame[..(4 + 60)];
        using var stream = new ChunkedReadStream(truncated, maxChunkSize: 4096);

        await Assert.ThrowsAsync<EndOfStreamException>(async () =>
            await LengthPrefixedProtocol.ReadFrameAsync(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadFrameAsync_Throws_WhenPayloadLengthExceedsMax()
    {
        // FF FF FF FF のような巨大・不正な長さ指定をそのまま確保しない
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF];
        using var stream = new ChunkedReadStream(data, maxChunkSize: 4096);

        await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await LengthPrefixedProtocol.ReadFrameAsync(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadFrameAsync_AllowsZeroLengthPayload()
    {
        byte[] frame = BuildFrame([]);
        using var stream = new ChunkedReadStream(frame, maxChunkSize: 4096);

        byte[]? payload = await LengthPrefixedProtocol.ReadFrameAsync(
            stream,
            CancellationToken.None);

        Assert.NotNull(payload);
        Assert.Empty(payload);
    }

    [Fact]
    public async Task WriteFrameAsync_RoundTripsWithReader()
    {
        byte[] payload = Encoding.UTF8.GetBytes("""{"message":"こんにちは"}""");
        using var buffer = new MemoryStream();

        await LengthPrefixedProtocolWriter.WriteFrameAsync(
            buffer,
            payload,
            CancellationToken.None);

        using var stream = new ChunkedReadStream(buffer.ToArray(), maxChunkSize: 1);
        byte[]? roundTripped = await LengthPrefixedProtocol.ReadFrameAsync(
            stream,
            CancellationToken.None);

        Assert.NotNull(roundTripped);
        Assert.Equal(payload, roundTripped);
    }

    [Fact]
    public async Task WriteFrameAsync_Throws_WhenPayloadExceedsMax()
    {
        byte[] payload = new byte[LengthPrefixedProtocol.MaxPayloadSize + 1];
        using var buffer = new MemoryStream();

        await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await LengthPrefixedProtocolWriter.WriteFrameAsync(
                buffer,
                payload,
                CancellationToken.None));
    }
}
