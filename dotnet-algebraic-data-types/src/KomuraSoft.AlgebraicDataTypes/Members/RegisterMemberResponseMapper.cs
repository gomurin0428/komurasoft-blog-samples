namespace KomuraSoft.AlgebraicDataTypes.Members;

// 記事 20 章: 既存の API 境界ではすぐに DTO や旧形式へ変換する
// 外部インターフェースを変えずに、内部ロジックだけ先に強くする
public static class RegisterMemberResponseMapper
{
    public static RegisterMemberResponse ToResponse(RegisterMemberResult result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        return result.Match(
            registered => new RegisterMemberResponse
            {
                Success = true,
                MemberId = registered.MemberId.Value
            },
            duplicate => new RegisterMemberResponse
            {
                Success = false,
                ErrorCode = "DuplicateEmail",
                ErrorMessage = duplicate.Email + " は既に使われています。"
            },
            invalidCode => new RegisterMemberResponse
            {
                Success = false,
                ErrorCode = "InvalidInvitationCode",
                ErrorMessage = "招待コードが無効です。"
            });
    }
}
