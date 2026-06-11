using KomuraSoft.AlgebraicDataTypes.Classic;
using Xunit;

namespace KomuraSoft.AlgebraicDataTypes.Tests;

// 記事 4 章: sealed なクラス階層 + Match メソッド
public class CreateUserResultClassicTests
{
    [Fact]
    public void ファクトリは対応するケース型を返す()
    {
        Assert.IsType<CreateUserResult.Created>(
            CreateUserResult.Ok(new User("U-0001", "komura")));
        Assert.IsType<CreateUserResult.DuplicateEmail>(
            CreateUserResult.EmailAlreadyUsed("komura@example.com"));
        Assert.IsType<CreateUserResult.WeakPassword>(
            CreateUserResult.PasswordIsWeak("短すぎます。"));
        Assert.IsType<CreateUserResult.SystemFailure>(
            CreateUserResult.Failed("DB接続に失敗しました。"));
    }

    [Fact]
    public void 成功ケースだけがUserを持つ()
    {
        var user = new User("U-0001", "komura");

        CreateUserResult result = CreateUserResult.Ok(user);

        var created = Assert.IsType<CreateUserResult.Created>(result);
        Assert.Same(user, created.User);
    }

    [Fact]
    public void MatchはCreatedケースを処理する()
    {
        CreateUserResult result = CreateUserResult.Ok(new User("U-0001", "komura"));

        string message = result.Match(
            created => "ユーザーを作成しました: " + created.User.Id,
            duplicate => "このメールアドレスは既に使われています: " + duplicate.Email,
            weak => "パスワードが弱すぎます: " + weak.Reason,
            failure => "ユーザー作成に失敗しました: " + failure.Message);

        Assert.Equal("ユーザーを作成しました: U-0001", message);
    }

    [Fact]
    public void MatchはDuplicateEmailケースを処理する()
    {
        CreateUserResult result = CreateUserResult.EmailAlreadyUsed("komura@example.com");

        string message = result.Match(
            created => "created",
            duplicate => "duplicate: " + duplicate.Email,
            weak => "weak",
            failure => "failure");

        Assert.Equal("duplicate: komura@example.com", message);
    }

    [Fact]
    public void MatchはWeakPasswordケースを処理する()
    {
        CreateUserResult result = CreateUserResult.PasswordIsWeak("12文字以上にしてください。");

        string message = result.Match(
            created => "created",
            duplicate => "duplicate",
            weak => "weak: " + weak.Reason,
            failure => "failure");

        Assert.Equal("weak: 12文字以上にしてください。", message);
    }

    [Fact]
    public void MatchはSystemFailureケースを処理する()
    {
        CreateUserResult result = CreateUserResult.Failed("タイムアウトしました。");

        string message = result.Match(
            created => "created",
            duplicate => "duplicate",
            weak => "weak",
            failure => "failure: " + failure.Message);

        Assert.Equal("failure: タイムアウトしました。", message);
    }
}
