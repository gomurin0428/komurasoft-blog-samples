namespace KomuraSoft.AlgebraicDataTypes.Members;

// 記事 20 章: 既存の API 境界で使われている旧形式のレスポンス
public sealed class RegisterMemberResponse
{
    public bool Success { get; set; }
    public string? MemberId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}
