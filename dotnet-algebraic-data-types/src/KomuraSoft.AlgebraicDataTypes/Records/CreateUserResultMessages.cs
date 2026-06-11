namespace KomuraSoft.AlgebraicDataTypes.Records;

// 記事 6 章: パターンマッチングと switch 式による利用例
public static class CreateUserResultMessages
{
    public static string ToMessage(CreateUserResult result)
    {
        return result switch
        {
            CreateUserResult.Created { User: var user }
                => $"ユーザーを作成しました: {user.Id}",

            CreateUserResult.DuplicateEmail { Email: var email }
                => $"このメールアドレスは既に使われています: {email}",

            CreateUserResult.WeakPassword { Reason: var reason }
                => $"パスワードが弱すぎます: {reason}",

            CreateUserResult.SystemFailure { Message: var message }
                => $"ユーザー作成に失敗しました: {message}",

            _ => throw new InvalidOperationException("未知の結果です。")
        };
    }
}
