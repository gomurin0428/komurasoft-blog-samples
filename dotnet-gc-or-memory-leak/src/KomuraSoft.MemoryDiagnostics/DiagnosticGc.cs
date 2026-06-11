namespace KomuraSoft.MemoryDiagnostics;

/// <summary>
/// 調査専用のフル GC 誘発ヘルパー（記事 7 章）。
/// 「強制 GC 後も残るか」を見るためのものであり、解決策として本番コードに入れてはいけません。
/// </summary>
public static class DiagnosticGc
{
    public static void ForceFullGcForDiagnosticsOnly()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
