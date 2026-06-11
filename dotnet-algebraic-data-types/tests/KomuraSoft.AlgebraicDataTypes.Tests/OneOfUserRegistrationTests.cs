using KomuraSoft.AlgebraicDataTypes.OneOfSamples;
using Xunit;

namespace KomuraSoft.AlgebraicDataTypes.Tests;

// 記事 8 章: OneOf のようなライブラリを使う
public class OneOfUserRegistrationTests
{
    [Fact]
    public void 正常な入力ならUserを返す()
    {
        var service = new UserRegistrationService();

        var result = service.CreateUser(
            new CreateUserCommand("new-user@example.com", "long-enough-password"));

        Assert.True(result.IsT0);
        Assert.Equal("new-user@example.com", result.AsT0.Name);
    }

    [Fact]
    public void 登録済みのメールならDuplicateEmailを返す()
    {
        var service = new UserRegistrationService(existingEmails: ["taken@example.com"]);

        var result = service.CreateUser(
            new CreateUserCommand("taken@example.com", "long-enough-password"));

        Assert.True(result.IsT1);
        Assert.Equal("taken@example.com", result.AsT1.Email);
    }

    [Fact]
    public void 短いパスワードならWeakPasswordを返す()
    {
        var service = new UserRegistrationService();

        var result = service.CreateUser(
            new CreateUserCommand("new-user@example.com", "short"));

        Assert.True(result.IsT2);
        Assert.Equal("12文字以上にしてください。", result.AsT2.Reason);
    }

    [Fact]
    public void 呼び出し側はMatchで全ケースを処理できる()
    {
        var service = new UserRegistrationService(existingEmails: ["taken@example.com"]);

        var result = service.CreateUser(
            new CreateUserCommand("taken@example.com", "long-enough-password"));

        var message = result.Match(
            user => $"作成しました: {user.Id}",
            duplicate => $"重複しています: {duplicate.Email}",
            weak => $"パスワードが弱いです: {weak.Reason}");

        Assert.Equal("重複しています: taken@example.com", message);
    }
}
