namespace KomuraSoft.MemoryDiagnostics;

/// <summary>
/// AsyncLocal によるコンテキスト保持（記事 11.6 章）。
/// 入れるものは小さく、明確にし、不要になったら null に戻す設計を検討します。
/// 大きな DTO やリクエストボディを入れると、非同期フローに乗って意図せず長生きします。
/// </summary>
public static class RequestContext
{
    public static readonly AsyncLocal<RequestInfo?> Current = new();
}
