using KomuraSoft.AlgebraicDataTypes.Records;
using Xunit;

namespace KomuraSoft.AlgebraicDataTypes.Tests;

// 記事 6 章: record 階層 + switch 式 + Match
public class CreateUserResultRecordTests
{
    [Fact]
    public void ToMessageは全ケースをswitch式で処理する()
    {
        var user = new User("U-0002", "tanaka");

        Assert.Equal(
            "ユーザーを作成しました: U-0002",
            CreateUserResultMessages.ToMessage(new CreateUserResult.Created(user)));
        Assert.Equal(
            "このメールアドレスは既に使われています: tanaka@example.com",
            CreateUserResultMessages.ToMessage(new CreateUserResult.DuplicateEmail("tanaka@example.com")));
        Assert.Equal(
            "パスワードが弱すぎます: 短すぎます。",
            CreateUserResultMessages.ToMessage(new CreateUserResult.WeakPassword("短すぎます。")));
        Assert.Equal(
            "ユーザー作成に失敗しました: タイムアウトしました。",
            CreateUserResultMessages.ToMessage(new CreateUserResult.SystemFailure("タイムアウトしました。")));
    }

    [Fact]
    public void Matchは全ケースの処理を呼び出し側に強制する()
    {
        var user = new User("U-0002", "tanaka");
        CreateUserResult result = new CreateUserResult.Created(user);

        var message = result.Match(
            created => $"作成しました: {created.User.Id}",
            duplicate => $"重複しています: {duplicate.Email}",
            weak => $"パスワードが弱いです: {weak.Reason}",
            failure => $"失敗しました: {failure.Message}");

        Assert.Equal("作成しました: U-0002", message);
    }

    [Fact]
    public void recordなので同じデータのケースは等価になる()
    {
        Assert.Equal(
            new CreateUserResult.DuplicateEmail("a@example.com"),
            new CreateUserResult.DuplicateEmail("a@example.com"));
        Assert.NotEqual(
            new CreateUserResult.DuplicateEmail("a@example.com"),
            new CreateUserResult.DuplicateEmail("b@example.com"));
    }
}
