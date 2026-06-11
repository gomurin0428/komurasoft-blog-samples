namespace KomuraSoft.AsyncPatterns;

/// <summary>
/// CPU 負荷の重い計算を Task.Run で今のスレッドから外す例です（記事 3.3）。
/// UI イベントハンドラなどから呼ぶと、計算中も UI スレッドを塞ぎません。
/// ASP.NET Core のリクエスト処理では、Task.Run をすぐ await する書き方は基本的に避けます。
/// </summary>
public static class CpuBoundHasher
{
    public static Task<byte[]> HashManyTimesAsync(byte[] data, int repeat, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] current = data;

            for (int i = 0; i < repeat; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                current = sha256.ComputeHash(current);
            }

            return current;
        }, cancellationToken);
    }
}
