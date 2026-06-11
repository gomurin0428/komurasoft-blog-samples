namespace KomuraSoft.FileWatching;

/// <summary>
/// bundle directory の rename による原子的な所有権取得です（記事 4.3 節）。
/// incoming -&gt; processing/&lt;worker&gt;/ への rename は、先に成功した 1 ワーカーだけが所有権を持ちます。
/// </summary>
public static class AtomicClaim
{
    /// <summary>
    /// bundle directory を rename して claim を試みます。
    /// 他ワーカーが先に rename していた場合などは false を返します。
    /// </summary>
    public static bool TryClaimByRename(string bundlePath, string claimedPath)
    {
        try
        {
            Directory.Move(bundlePath, claimedPath);
            return true;
        }
        catch (IOException)
        {
            // 他ワーカーが先に取得した（移動元が消えた、移動先が既にある、など）
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
