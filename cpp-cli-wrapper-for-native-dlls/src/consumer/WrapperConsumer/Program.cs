// 記事 6.3 の「C# 側はかなり素直になります」のコードです。
// C# から見えるのは string / List<int> / IDisposable だけで、
// IntPtr や解放関数やネイティブ文字列バッファの都合は見えません。
//
// 実行には、先に AnalyzerWrapper.vcxproj（C++/CLI、Windows 専用）を
// ビルドしておく必要があります。手順は README.md を参照してください。

using var analyzer = new AnalyzerWrapper(@"C:\license.dat");

var result = analyzer.Analyze(
    @"C:\input.png",
    new AnalysisOptions
    {
        Threshold = 80,
        ModelPath = @"C:\model.bin"
    });

if (!result.Ok)
{
    Console.WriteLine(result.Message);
}

// 記事のコードはここまで。以降はサンプルとして結果を表示する追加コードです。
Console.WriteLine($"Ok      : {result.Ok}");
Console.WriteLine($"Message : {result.Message}");
Console.WriteLine($"Scores  : [{string.Join(", ", result.Scores)}]");
