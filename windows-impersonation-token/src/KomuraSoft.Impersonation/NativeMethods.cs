using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace KomuraSoft.Impersonation;

/// <summary>
/// 偽装に使う Win32 API の P/Invoke 宣言です（記事 8 章・10 章・12 章）。
/// </summary>
/// <remarks>
/// <c>LogonUser</c> はアプリケーション側が資格情報を扱うことになるため、慎重に扱うべき API です。
/// 実務では、まずサービスアカウント、Windows 認証、委任などの代替案を検討してください（記事 10 章）。
/// </remarks>
internal static class NativeMethods
{
    private const int LOGON32_LOGON_INTERACTIVE = 2;
    private const int LOGON32_PROVIDER_DEFAULT = 0;

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool LogonUser(
        string lpszUsername,
        string? lpszDomain,
        string lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out SafeAccessTokenHandle phToken);

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool ImpersonateLoggedOnUser(SafeAccessTokenHandle hToken);

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool RevertToSelf();

    /// <summary>
    /// ユーザー名・ドメイン・パスワードから偽装に使うトークンを取得します（記事 10 章）。
    /// </summary>
    /// <remarks>
    /// ログオン種別によって、返るトークンの性質（ローカルアクセス、ネットワークアクセス、
    /// プロセス起動での挙動）が変わる点に注意してください（記事 11 章）。
    /// </remarks>
    public static SafeAccessTokenHandle Logon(
        string userName,
        string? domain,
        string password)
    {
        bool ok = LogonUser(
            userName,
            domain,
            password,
            LOGON32_LOGON_INTERACTIVE,
            LOGON32_PROVIDER_DEFAULT,
            out SafeAccessTokenHandle token);

        if (!ok)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return token;
    }
}
