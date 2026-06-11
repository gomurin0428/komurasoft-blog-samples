// 記事 6.2 の「C# 側も、こんな雰囲気になります」のコードです。
// SafeAnalyzerHandle / AnalyzeOptionsNative / NativeMethods は記事のままです。
// AnalyzeResultNative の定義は記事では省略されているため、
// ネイティブ側（src/native/NativeBridge.hpp）に合わせてサンプル側で補っています。

using System.Runtime.InteropServices;

internal sealed class SafeAnalyzerHandle : SafeHandle
{
    private SafeAnalyzerHandle() : base(IntPtr.Zero, ownsHandle: true) { }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        NativeMethods.Analyzer_Destroy(handle);
        return true;
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct AnalyzeOptionsNative
{
    public int Threshold;
    public IntPtr ModelPath;
}

// 記事では省略されている結果構造体。NativeBridge.hpp の AnalyzeResultNative と
// メモリレイアウトを一致させる必要がある（固定長バッファのサイズも含めて）。
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct AnalyzeResultNative
{
    public int Ok; // 0 = false, 1 = true

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string Message;

    public int ScoreCount;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public int[] Scores;
}

internal static class NativeMethods
{
    [DllImport("NativeBridge.dll", CharSet = CharSet.Unicode)]
    internal static extern SafeAnalyzerHandle Analyzer_Create(string licensePath);

    [DllImport("NativeBridge.dll", CharSet = CharSet.Unicode)]
    internal static extern void Analyzer_Destroy(IntPtr handle);

    [DllImport("NativeBridge.dll", CharSet = CharSet.Unicode)]
    internal static extern int Analyzer_Analyze(
        SafeAnalyzerHandle handle,
        string imagePath,
        ref AnalyzeOptionsNative options,
        out AnalyzeResultNative result);
}
