using KomuraSoft.AlgebraicDataTypes.Members;
using Xunit;

namespace KomuraSoft.AlgebraicDataTypes.Tests;

// 記事 20 章: 既存の API 境界ではすぐに旧形式へ変換する
public class RegisterMemberResponseMapperTests
{
    [Fact]
    public void Registeredは成功レスポンスに変換される()
    {
        var result = RegisterMemberResult.Success(new MemberId("M-0001"));

        var response = RegisterMemberResponseMapper.ToResponse(result);

        Assert.True(response.Success);
        Assert.Equal("M-0001", response.MemberId);
        Assert.Null(response.ErrorCode);
    }

    [Fact]
    public void DuplicateEmailは旧形式のエラーコードに変換される()
    {
        var result = RegisterMemberResult.EmailAlreadyUsed("komura@example.com");

        var response = RegisterMemberResponseMapper.ToResponse(result);

        Assert.False(response.Success);
        Assert.Equal("DuplicateEmail", response.ErrorCode);
        Assert.Equal("komura@example.com は既に使われています。", response.ErrorMessage);
    }

    [Fact]
    public void InvalidInvitationCodeは旧形式のエラーコードに変換される()
    {
        var result = RegisterMemberResult.InvitationCodeIsInvalid("INVALID");

        var response = RegisterMemberResponseMapper.ToResponse(result);

        Assert.False(response.Success);
        Assert.Equal("InvalidInvitationCode", response.ErrorCode);
        Assert.Equal("招待コードが無効です。", response.ErrorMessage);
    }
}
