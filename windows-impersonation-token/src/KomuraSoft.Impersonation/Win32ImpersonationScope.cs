using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace KomuraSoft.Impersonation;

/// <summary>
/// Win32 API（<c>ImpersonateLoggedOnUser</c> / <c>RevertToSelf</c>）による偽装の基本形です（記事 8 章・12 章）。
/// </summary>
/// <remarks>
/// 大事なのは、偽装の開始よりも「確実に戻すこと」です。
/// <c>DoWork()</c> で例外が発生しても必ず <c>RevertToSelf</c> が呼ばれるよう、<c>try</c> / <c>finally</c> にします。
/// .NET で書くなら、まず <see cref="System.Security.Principal.WindowsIdentity.RunImpersonated"/> の利用を検討してください（記事 9 章）。
/// </remarks>
public static class Win32ImpersonationScope
{
    /// <summary>
    /// 現在のスレッドを指定したトークンで偽装し、<paramref name="action"/> の実行後に必ず元のコンテキストへ戻します。
    /// </summary>
    public static void Run(SafeAccessTokenHandle token, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        // SafeAccessTokenHandle は非 Windows ではそもそも生成できないため、
        // トークンの検証より先にプラットフォームを確認する
        PlatformGuard.ThrowIfNotWindows();
        ArgumentNullException.ThrowIfNull(token);

        if (!NativeMethods.ImpersonateLoggedOnUser(token))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            // ここだけ偽装ユーザーとして実行する
            action();
        }
        finally
        {
            if (!NativeMethods.RevertToSelf())
            {
                // 戻せない状態は危険なので、少なくとも処理継続してはいけない
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }
}
