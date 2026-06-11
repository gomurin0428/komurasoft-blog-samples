namespace KomuraSoft.Impersonation;

/// <summary>
/// 偽装トークンは Windows のアクセストークンを前提とした仕組みのため、
/// 非 Windows では分かりやすい例外で早期に失敗させます。
/// </summary>
internal static class PlatformGuard
{
    public static void ThrowIfNotWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Windows の偽装トークン（アクセストークン）は Windows 上でのみ使用できます。");
        }
    }
}
