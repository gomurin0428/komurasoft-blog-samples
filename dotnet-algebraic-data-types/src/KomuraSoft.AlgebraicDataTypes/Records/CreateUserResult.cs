namespace KomuraSoft.AlgebraicDataTypes.Records;

// 記事 6 章: 現行 .NET なら record 階層で簡潔に書ける
// abstract record + sealed record cases + Match メソッド
public abstract record CreateUserResult
{
    private CreateUserResult()
    {
    }

    public sealed record Created(User User) : CreateUserResult;
    public sealed record DuplicateEmail(string Email) : CreateUserResult;
    public sealed record WeakPassword(string Reason) : CreateUserResult;
    public sealed record SystemFailure(string Message) : CreateUserResult;

    public T Match<T>(
        Func<Created, T> created,
        Func<DuplicateEmail, T> duplicateEmail,
        Func<WeakPassword, T> weakPassword,
        Func<SystemFailure, T> systemFailure)
    {
        return this switch
        {
            Created x => created(x),
            DuplicateEmail x => duplicateEmail(x),
            WeakPassword x => weakPassword(x),
            SystemFailure x => systemFailure(x),
            _ => throw new InvalidOperationException("未知の結果です。")
        };
    }
}
