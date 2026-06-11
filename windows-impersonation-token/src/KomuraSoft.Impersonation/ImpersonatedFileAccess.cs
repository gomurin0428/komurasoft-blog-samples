using Microsoft.Win32.SafeHandles;
using System.Security.Principal;

namespace KomuraSoft.Impersonation;

/// <summary>
/// <see cref="WindowsIdentity.RunImpersonated"/> / <see cref="WindowsIdentity.RunImpersonatedAsync"/> を使い、
/// 偽装スコープをファイルアクセスだけに閉じ込める例です（記事 9 章・10 章・13 章）。
/// </summary>
public static class ImpersonatedFileAccess
{
    /// <summary>
    /// 指定したトークンのユーザーとしてファイルを読み取ります（記事 9 章）。
    /// </summary>
    /// <remarks>
    /// 偽装している範囲がラムダ式の中に閉じているため、どこまでが偽装ユーザーの権限で
    /// 動くのかがコード上で明確になります。
    /// </remarks>
    public static string ReadFileAsUser(SafeAccessTokenHandle token, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        // SafeAccessTokenHandle は非 Windows ではそもそも生成できないため、
        // トークンの検証より先にプラットフォームを確認する
        PlatformGuard.ThrowIfNotWindows();
        ArgumentNullException.ThrowIfNull(token);

        return WindowsIdentity.RunImpersonated(token, () =>
        {
            return File.ReadAllText(path);
        });
    }

    /// <summary>
    /// 指定したトークンのユーザーとしてファイルへ書き込みます（記事 9 章）。
    /// </summary>
    /// <remarks>
    /// 偽装して行いたい非同期処理は、<c>RunImpersonatedAsync</c> の中で <c>await</c> し、
    /// 処理が完了してからスコープを出る形にします。
    /// 偽装スコープの内側から fire-and-forget のタスクを投げてはいけません（記事 14 章・21 章）。
    /// </remarks>
    public static Task WriteFileAsUserAsync(
        SafeAccessTokenHandle token,
        string path,
        string text,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(text);
        // SafeAccessTokenHandle は非 Windows ではそもそも生成できないため、
        // トークンの検証より先にプラットフォームを確認する
        PlatformGuard.ThrowIfNotWindows();
        ArgumentNullException.ThrowIfNull(token);

        return WindowsIdentity.RunImpersonatedAsync(token, async () =>
        {
            await File.WriteAllTextAsync(path, text, cancellationToken);
        });
    }

    /// <summary>
    /// 明示的な資格情報からトークンを取得し、そのユーザーとしてファイルを読み取ります（記事 10 章）。
    /// </summary>
    /// <remarks>
    /// この例は、あくまで API の形を示すためのものです。
    /// 実務では「そもそもアプリケーションがパスワードを受け取る設計でよいのか」を先に検討してください。
    /// パスワードをコードや設定ファイルに平文で置かない、ログに秘密情報を出さない、
    /// トークンハンドルは <c>using</c> で必ず閉じる、といった点にも注意が必要です（記事 19 章・20 章）。
    /// </remarks>
    public static string ReadFileWithExplicitCredential(
        string userName,
        string? domain,
        string password,
        string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(userName);
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentException.ThrowIfNullOrEmpty(path);
        PlatformGuard.ThrowIfNotWindows();

        using SafeAccessTokenHandle token = NativeMethods.Logon(userName, domain, password);

        return WindowsIdentity.RunImpersonated(token, () =>
        {
            return File.ReadAllText(path);
        });
    }
}
