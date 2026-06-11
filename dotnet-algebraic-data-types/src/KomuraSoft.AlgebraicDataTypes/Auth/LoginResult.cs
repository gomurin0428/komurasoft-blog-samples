namespace KomuraSoft.AlgebraicDataTypes.Auth;

// 記事 12 章: Result型 ── 想定内の失敗を型で返す
// ログイン処理が取り得る結果を 1 つの閉じた集合として表す
public abstract class LoginResult
{
    private LoginResult()
    {
    }

    public sealed class Succeeded : LoginResult
    {
        internal Succeeded(Session session)
        {
            Session = session;
        }

        public Session Session { get; }
    }

    public sealed class InvalidPassword : LoginResult
    {
        internal InvalidPassword()
        {
        }
    }

    public sealed class AccountLocked : LoginResult
    {
        internal AccountLocked(DateTimeOffset until)
        {
            Until = until;
        }

        public DateTimeOffset Until { get; }
    }

    public sealed class MfaRequired : LoginResult
    {
        internal MfaRequired(string challengeId)
        {
            ChallengeId = challengeId;
        }

        public string ChallengeId { get; }
    }

    public static LoginResult Success(Session session)
        => new Succeeded(session);

    public static LoginResult WrongPassword()
        => new InvalidPassword();

    public static LoginResult Locked(DateTimeOffset until)
        => new AccountLocked(until);

    public static LoginResult RequireMfa(string challengeId)
        => new MfaRequired(challengeId);

    public T Match<T>(
        Func<Succeeded, T> succeeded,
        Func<InvalidPassword, T> invalidPassword,
        Func<AccountLocked, T> accountLocked,
        Func<MfaRequired, T> mfaRequired)
    {
        if (succeeded == null) throw new ArgumentNullException(nameof(succeeded));
        if (invalidPassword == null) throw new ArgumentNullException(nameof(invalidPassword));
        if (accountLocked == null) throw new ArgumentNullException(nameof(accountLocked));
        if (mfaRequired == null) throw new ArgumentNullException(nameof(mfaRequired));

        var s = this as Succeeded;
        if (s != null) return succeeded(s);

        var i = this as InvalidPassword;
        if (i != null) return invalidPassword(i);

        var l = this as AccountLocked;
        if (l != null) return accountLocked(l);

        var m = this as MfaRequired;
        if (m != null) return mfaRequired(m);

        throw new InvalidOperationException("Unknown result type: " + GetType().FullName);
    }
}
