using KomuraSoft.AlgebraicDataTypes.Auth;
using Xunit;

namespace KomuraSoft.AlgebraicDataTypes.Tests;

// 記事 12 章: Result型 ── 想定内の失敗を型で返す
public class LoginResultTests
{
    [Fact]
    public void Matchはログインの全結果ケースを処理できる()
    {
        var until = new DateTimeOffset(2026, 6, 9, 12, 0, 0, TimeSpan.FromHours(9));

        LoginResult[] results =
        [
            LoginResult.Success(new Session("SESSION-001")),
            LoginResult.WrongPassword(),
            LoginResult.Locked(until),
            LoginResult.RequireMfa("CHALLENGE-001"),
        ];

        string[] statuses = results
            .Select(result => result.Match(
                succeeded => "200 " + succeeded.Session.Token,
                invalidPassword => "401",
                accountLocked => "423 " + accountLocked.Until.ToString("HH:mm"),
                mfaRequired => "202 " + mfaRequired.ChallengeId))
            .ToArray();

        string[] expected = ["200 SESSION-001", "401", "423 12:00", "202 CHALLENGE-001"];
        Assert.Equal(expected, statuses);
    }

    [Fact]
    public void 各ケースは必要なデータだけを持つ()
    {
        var session = new Session("SESSION-001");

        var succeeded = Assert.IsType<LoginResult.Succeeded>(LoginResult.Success(session));
        Assert.Same(session, succeeded.Session);

        var locked = Assert.IsType<LoginResult.AccountLocked>(
            LoginResult.Locked(DateTimeOffset.MaxValue));
        Assert.Equal(DateTimeOffset.MaxValue, locked.Until);

        var mfa = Assert.IsType<LoginResult.MfaRequired>(LoginResult.RequireMfa("C-1"));
        Assert.Equal("C-1", mfa.ChallengeId);
    }
}
