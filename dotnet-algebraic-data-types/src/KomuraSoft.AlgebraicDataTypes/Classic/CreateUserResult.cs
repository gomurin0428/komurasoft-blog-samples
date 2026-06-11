namespace KomuraSoft.AlgebraicDataTypes.Classic;

// 記事 4 章: .NET Framework でも使える実装
// 抽象基底クラス + private コンストラクター + ネストした sealed クラス + Match メソッド
public abstract class CreateUserResult
{
    private CreateUserResult()
    {
    }

    public sealed class Created : CreateUserResult
    {
        internal Created(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            User = user;
        }

        public User User { get; }
    }

    public sealed class DuplicateEmail : CreateUserResult
    {
        internal DuplicateEmail(string email)
        {
            if (email == null) throw new ArgumentNullException(nameof(email));
            Email = email;
        }

        public string Email { get; }
    }

    public sealed class WeakPassword : CreateUserResult
    {
        internal WeakPassword(string reason)
        {
            if (reason == null) throw new ArgumentNullException(nameof(reason));
            Reason = reason;
        }

        public string Reason { get; }
    }

    public sealed class SystemFailure : CreateUserResult
    {
        internal SystemFailure(string message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            Message = message;
        }

        public string Message { get; }
    }

    public static CreateUserResult Ok(User user)
        => new Created(user);

    public static CreateUserResult EmailAlreadyUsed(string email)
        => new DuplicateEmail(email);

    public static CreateUserResult PasswordIsWeak(string reason)
        => new WeakPassword(reason);

    public static CreateUserResult Failed(string message)
        => new SystemFailure(message);

    public T Match<T>(
        Func<Created, T> created,
        Func<DuplicateEmail, T> duplicateEmail,
        Func<WeakPassword, T> weakPassword,
        Func<SystemFailure, T> systemFailure)
    {
        if (created == null) throw new ArgumentNullException(nameof(created));
        if (duplicateEmail == null) throw new ArgumentNullException(nameof(duplicateEmail));
        if (weakPassword == null) throw new ArgumentNullException(nameof(weakPassword));
        if (systemFailure == null) throw new ArgumentNullException(nameof(systemFailure));

        var c = this as Created;
        if (c != null) return created(c);

        var d = this as DuplicateEmail;
        if (d != null) return duplicateEmail(d);

        var w = this as WeakPassword;
        if (w != null) return weakPassword(w);

        var f = this as SystemFailure;
        if (f != null) return systemFailure(f);

        throw new InvalidOperationException("Unknown result type: " + GetType().FullName);
    }
}
