using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Inspection;

[StructLayout(LayoutKind.Sequential)]
public struct InspectParams
{
    public int Threshold;        // これより暗い画素を欠陥とみなす（0〜255）
    public int MaxDefectPixels;  // 欠陥の画素数がこれを超えたら不合格
}

[StructLayout(LayoutKind.Sequential)]
public struct InspectResult
{
    public long DefectPixels;
    public int MinX, MinY, MaxX, MaxY;  // 欠陥の外接矩形。欠陥がなければ -1
    public int Verdict;                 // 0 = 合格、1 = 不合格
}

public enum InspectStatus
{
    Ok = 0,
    NullPointer = 1,
    BadGeometry = 2,
    BufferTooSmall = 3,
    BadParameter = 4,
    InternalError = 99,
}

public static unsafe partial class AdaInspector
{
    // Windows では libinspect.dll、Linux では libinspect.so を探す
    private const string Lib = "libinspect";

    [LibraryImport(Lib, EntryPoint = "inspect_dark_defects")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial int InspectDarkDefects(
        byte* pixels, nuint length, int width, int height, int stride,
        InspectParams* parameters, InspectResult* result);

    [LibraryImport(Lib, EntryPoint = "inspect_result_size")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static partial int NativeResultSize();

    [LibraryImport(Lib, EntryPoint = "inspect_elaborated")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static partial int Elaborated();

    public static InspectStatus Inspect(
        ReadOnlySpan<byte> pixels, int width, int height, int stride,
        InspectParams parameters, out InspectResult result)
    {
        InspectResult r;
        int status;

        // fixed の中だけ、GC が配列を動かさない。Ada はこの呼び出しの間だけ読む
        fixed (byte* p = pixels)
        {
            status = InspectDarkDefects(
                p, (nuint)pixels.Length, width, height, stride, &parameters, &r);
        }

        result = r;
        return (InspectStatus)status;
    }
}
