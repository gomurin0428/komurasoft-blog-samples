using KomuraSoft.RoslynSamples.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace KomuraSoft.RoslynSamples.Tests;

/// <summary>
/// Source Generator は「入力ソースを用意 → Generator を実行 → 生成されたソースを確認」
/// という形でテストする（記事 30 章）。
/// </summary>
public class BuildInfoGeneratorTests
{
    private static (Compilation OutputCompilation, GeneratorDriverRunResult RunResult) RunGenerator(
        string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorSample",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BuildInfoGenerator());

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out _);

        return (outputCompilation, driver.GetRunResult());
    }

    [Fact]
    public void Generator_AddsBuildInfoSource()
    {
        var (_, runResult) = RunGenerator("class C { }");

        var generated = Assert.Single(Assert.Single(runResult.Results).GeneratedSources);

        Assert.Equal("BuildInfo.g.cs", generated.HintName);
        Assert.Contains("namespace Generated;", generated.SourceText.ToString());
        Assert.Contains("public static class BuildInfo", generated.SourceText.ToString());
    }

    [Fact]
    public void Generator_OutputCompilationHasNoErrors()
    {
        // 生成コードを加えたコンパイルがエラーなく成立すること
        var (outputCompilation, _) = RunGenerator("class C { }");

        Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
        Assert.Empty(outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Generator_GeneratedTypeIsUsableFromUserCode()
    {
        // 生成された Generated.BuildInfo.Tool をユーザーコードから参照できること（記事 17 章）
        var source = """
class C
{
    public static string Tool() => Generated.BuildInfo.Tool;
}
""";

        var (outputCompilation, _) = RunGenerator(source);

        Assert.Empty(outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Generator_GeneratesExpectedToolName()
    {
        // 生成結果は決定的であるべき（記事 19 章）。期待する内容をそのまま確認する
        var (_, runResult) = RunGenerator("class C { }");
        var generated = runResult.Results[0].GeneratedSources[0].SourceText.ToString();

        Assert.Contains("""public static string Tool => "Roslyn Source Generator";""", generated);
    }
}
