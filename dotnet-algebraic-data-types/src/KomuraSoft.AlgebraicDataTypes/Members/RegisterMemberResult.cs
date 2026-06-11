namespace KomuraSoft.AlgebraicDataTypes.Members;

// 記事 20 章: .NET Framework 向けの導入方針
// まず戻り値から始める ── 専用の結果型を作る
public abstract class RegisterMemberResult
{
    private RegisterMemberResult()
    {
    }

    public sealed class Registered : RegisterMemberResult
    {
        internal Registered(MemberId memberId)
        {
            MemberId = memberId;
        }

        public MemberId MemberId { get; }
    }

    public sealed class DuplicateEmail : RegisterMemberResult
    {
        internal DuplicateEmail(string email)
        {
            Email = email;
        }

        public string Email { get; }
    }

    public sealed class InvalidInvitationCode : RegisterMemberResult
    {
        internal InvalidInvitationCode(string code)
        {
            Code = code;
        }

        public string Code { get; }
    }

    public static RegisterMemberResult Success(MemberId memberId)
        => new Registered(memberId);

    public static RegisterMemberResult EmailAlreadyUsed(string email)
        => new DuplicateEmail(email);

    public static RegisterMemberResult InvitationCodeIsInvalid(string code)
        => new InvalidInvitationCode(code);

    public T Match<T>(
        Func<Registered, T> registered,
        Func<DuplicateEmail, T> duplicateEmail,
        Func<InvalidInvitationCode, T> invalidInvitationCode)
    {
        if (registered == null) throw new ArgumentNullException(nameof(registered));
        if (duplicateEmail == null) throw new ArgumentNullException(nameof(duplicateEmail));
        if (invalidInvitationCode == null) throw new ArgumentNullException(nameof(invalidInvitationCode));

        var r = this as Registered;
        if (r != null) return registered(r);

        var d = this as DuplicateEmail;
        if (d != null) return duplicateEmail(d);

        var i = this as InvalidInvitationCode;
        if (i != null) return invalidInvitationCode(i);

        throw new InvalidOperationException("Unknown result type: " + GetType().FullName);
    }
}
