using Xunit;

namespace KomuraSoft.AlgebraicDataTypes.Tests;

// 記事 11 章: Option型 ── null の代わりに「ない」を表す
public class OptionTests
{
    [Fact]
    public void 値があるならSomeになる()
    {
        var user = new User("U-0003", "suzuki");

        Option<User> option = Option<User>.Of(user);

        Assert.IsType<Option<User>.Some>(option);
    }

    [Fact]
    public void nullを渡すとNoneになる()
    {
        User? user = null;

        Option<User> option = Option<User>.Of(user!);

        Assert.IsType<Option<User>.None>(option);
    }

    [Fact]
    public void EmptyはNoneを返す()
    {
        Assert.IsType<Option<User>.None>(Option<User>.Empty());
    }

    [Fact]
    public void MatchはSomeの値を取り出せる()
    {
        Option<User> user = Option<User>.Of(new User("U-0003", "suzuki"));

        string displayName = user.Match(
            some: u => u.Name,
            none: () => "ゲスト");

        Assert.Equal("suzuki", displayName);
    }

    [Fact]
    public void MatchはNoneで既定の処理に落ちる()
    {
        Option<User> user = Option<User>.Empty();

        string displayName = user.Match(
            some: u => u.Name,
            none: () => "ゲスト");

        Assert.Equal("ゲスト", displayName);
    }
}
