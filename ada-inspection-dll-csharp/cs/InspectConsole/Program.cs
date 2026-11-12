using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Inspection;

// 追加の確かめ (probe-nre / probe-badptr) は、引数で1つだけ実行する
if (args.Length > 0)
{
    return Probes.Run(args[0]);
}

int failures = 0;

// 構造体の大きさを両側で比べ、食い違えば検査を始めない
int managedSize;
unsafe { managedSize = sizeof(InspectResult); }
int nativeSize = AdaInspector.NativeResultSize();
Console.WriteLine($"elaborated={AdaInspector.Elaborated()} result size: C#={managedSize} Ada={nativeSize}");
if (AdaInspector.Elaborated() != 1 || managedSize != nativeSize)
{
    Console.WriteLine("layout mismatch");
    return 1;
}

const int W = 640, H = 480;
var p = new InspectParams { Threshold = 64, MaxDefectPixels = 50 };

byte[] frame = MakeFrame(W, H, W, 200, 200);
DrawSquare(frame, W, 100, 50, 10, 20);
Check("ng (max 50)", AdaInspector.Inspect(frame, W, H, W, p, out var r), r, InspectStatus.Ok, 100, 1);
p.MaxDefectPixels = 200;
Check("ok (max 200)", AdaInspector.Inspect(frame, W, H, W, p, out r), r, InspectStatus.Ok, 100, 0);

// stride が幅より大きく、詰め物の部分が 0（暗い）
const int Stride = 644;
byte[] padded = MakeFrame(W, H, Stride, 200, 0);
DrawSquare(padded, Stride, 100, 50, 10, 20);
Check("padded stride", AdaInspector.Inspect(padded, W, H, Stride, p, out r), r, InspectStatus.Ok, 100, 0);
Check("buffer too small", AdaInspector.Inspect(padded.AsSpan(0, Stride * H - 1), W, H, Stride, p, out r), r, InspectStatus.BufferTooSmall);
Check("width > stride", AdaInspector.Inspect(padded, 700, H, Stride, p, out r), r, InspectStatus.BadGeometry);
Check("threshold 300", AdaInspector.Inspect(padded, W, H, Stride, p with { Threshold = 300 }, out r), r, InspectStatus.BadParameter);
Check("max -1", AdaInspector.Inspect(padded, W, H, Stride, p with { MaxDefectPixels = -1 }, out r), r, InspectStatus.BadParameter);
Check("empty span", AdaInspector.Inspect(ReadOnlySpan<byte>.Empty, W, H, Stride, p, out r), r, InspectStatus.NullPointer);

Console.WriteLine(failures == 0 ? "ALL PASSED" : $"FAILURES: {failures}");
return failures == 0 ? 0 : 1;

static byte[] MakeFrame(int width, int height, int stride, byte background, byte padding)
{
    var buffer = new byte[stride * height];
    for (int y = 0; y < height; y++)
    {
        var row = buffer.AsSpan(y * stride, stride);
        row[..width].Fill(background);
        row[width..].Fill(padding);
    }
    return buffer;
}

static void DrawSquare(byte[] buffer, int stride, int x0, int y0, int size, byte value)
{
    for (int y = y0; y < y0 + size; y++)
    {
        buffer.AsSpan(y * stride + x0, size).Fill(value);
    }
}

void Check(string name, InspectStatus status, InspectResult result,
           InspectStatus expected, long defects = -1, int verdict = -1)
{
    Console.WriteLine($"{name,-18} status={status} defects={result.DefectPixels} " +
        $"bbox=({result.MinX},{result.MinY})-({result.MaxX},{result.MaxY}) verdict={result.Verdict}");
    if (status != expected
        || (defects >= 0 && result.DefectPixels != defects)
        || (verdict >= 0 && result.Verdict != verdict))
    {
        Console.WriteLine("  UNEXPECTED");
        failures++;
    }
}

// 記事の本文では使わない、追加の確かめ
static unsafe class Probes
{
    private sealed class Box
    {
        public int Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Box? NullBox() => null;

    [DllImport("libinspect", EntryPoint = "inspect_dark_defects",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern int RawInspect(
        byte* pixels, nuint length, int width, int height, int stride,
        InspectParams* parameters, InspectResult* result);

    public static int Run(string name)
    {
        switch (name)
        {
            case "probe-nre":
                // ライブラリを読み込んで初期化させてから、マネージドのコードで null 参照を起こす
                Console.WriteLine($"elaborated={AdaInspector.Elaborated()}");
                try
                {
                    Console.WriteLine(NullBox()!.Value);
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine("managed NullReferenceException caught");
                }
                return 0;

            case "probe-badptr":
                // 読めないアドレスと、寸法に合わせた偽りの長さを渡す
                var p = new InspectParams { Threshold = 64, MaxDefectPixels = 50 };
                InspectResult r;
                int status = RawInspect((byte*)0x1000, 640 * 480, 640, 480, 640, &p, &r);
                Console.WriteLine($"bad pointer status={status}");
                return 0;

            default:
                Console.WriteLine($"unknown probe: {name}");
                return 2;
        }
    }
}
