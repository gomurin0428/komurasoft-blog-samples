using System.Buffers;

namespace KomuraSoft.MemoryDiagnostics;

/// <summary>
/// ArrayPool の貸し借り（記事 12 章）。
/// Rent した配列は必ず Return します。
/// ただし、プールに返したからといって、プロセスのメモリがすぐ下がるとは限りません。
/// </summary>
public static class PooledBuffers
{
    public static void ProcessWithPooledBuffer(int minimumLength, Action<byte[]> use)
    {
        var pool = ArrayPool<byte>.Shared;
        var buffer = pool.Rent(minimumLength);

        try
        {
            use(buffer);
        }
        finally
        {
            pool.Return(buffer);
        }
    }
}
