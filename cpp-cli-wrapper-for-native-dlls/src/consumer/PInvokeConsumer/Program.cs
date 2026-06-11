// P/Invoke 版の消費側コードです。
// C++/CLI 版（WrapperConsumer/Program.cs）と見比べると、
// IntPtr の手動マーシャリング、エラーコードの解釈、固定長バッファの切り出しなど、
// 「境界面の都合」が C# 側に漏れてくることが分かります。
//
// 実行には NativeBridge.dll（src/native/NativeBridge.vcxproj、Windows 専用）が必要です。

using System.Runtime.InteropServices;

using SafeAnalyzerHandle analyzer = NativeMethods.Analyzer_Create(@"C:\license.dat");

if (analyzer.IsInvalid)
{
    // 生成失敗の理由（例外の詳細）は C 境界で失われていて、ここでは分からない。
    Console.WriteLine("Analyzer_Create failed.");
    return;
}

// ModelPath は IntPtr なので、呼び出し側がネイティブメモリを確保・解放する。
IntPtr modelPath = Marshal.StringToHGlobalUni(@"C:\model.bin");

try
{
    var options = new AnalyzeOptionsNative
    {
        Threshold = 80,
        ModelPath = modelPath,
    };

    int error = NativeMethods.Analyzer_Analyze(
        analyzer,
        @"C:\input.png",
        ref options,
        out AnalyzeResultNative result);

    if (error != 0)
    {
        // エラーコードの意味は、呼び出し側がネイティブ側の規約を知らないと解釈できない。
        Console.WriteLine($"Analyzer_Analyze failed with error code {error}.");
        return;
    }

    // 固定長バッファから有効な範囲だけを切り出す。
    int[] scores = result.Scores.AsSpan(0, result.ScoreCount).ToArray();

    Console.WriteLine($"Ok      : {result.Ok != 0}");
    Console.WriteLine($"Message : {result.Message}");
    Console.WriteLine($"Scores  : [{string.Join(", ", scores)}]");
}
finally
{
    Marshal.FreeHGlobal(modelPath);
}
