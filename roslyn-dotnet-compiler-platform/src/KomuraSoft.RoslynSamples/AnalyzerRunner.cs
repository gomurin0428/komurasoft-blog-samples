using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KomuraSoft.RoslynSamples;

/// <summary>
/// DiagnosticAnalyzer を単一ソースに対して実行するヘルパーです。
/// Analyzer のテスト（記事 31 章）で、検出すべきコード・検出してはいけないコードを検証するために使います。
/// </summary>
public static class AnalyzerRunner
{
    /// <summary>
    /// ソースコードをコンパイルし、指定した Analyzer が報告した Diagnostic を返します。
    /// </summary>
    public static async Task<ImmutableArray<Diagnostic>> RunAsync(
        DiagnosticAnalyzer analyzer,
        string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);

        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerSample",
            syntaxTrees: new[] { tree },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create(analyzer));

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
