using Xunit;

namespace KomuraSoft.Impersonation.Tests;

public class Win32ImpersonationScopeTests
{
    [Fact]
    public void Run_NullAction_ThrowsArgumentNullException()
    {
        // action はトークンより先に検証される（SafeAccessTokenHandle は非 Windows では生成できないため）
        Assert.Throws<ArgumentNullException>(
            () => Win32ImpersonationScope.Run(null!, null!));
    }

    [Fact]
    public void Run_OnWindows_NullToken_ThrowsArgumentNullException()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // 非 Windows ではトークン検証より先にプラットフォームガードが働く
        }

        Assert.Throws<ArgumentNullException>(
            () => Win32ImpersonationScope.Run(null!, () => { }));
    }

    [Fact]
    public void Run_OnNonWindows_ThrowsPlatformNotSupportedException()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // Windows では ImpersonateLoggedOnUser の呼び出しへ進むため、このガードは対象外
        }

        bool executed = false;

        Assert.Throws<PlatformNotSupportedException>(
            () => Win32ImpersonationScope.Run(null!, () => executed = true));

        // ガードで失敗した場合、偽装スコープの中身は一切実行されないこと
        Assert.False(executed);
    }
}
