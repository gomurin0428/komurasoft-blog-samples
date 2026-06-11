using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace KomuraSoft.RoslynSamples.Analyzers;

/// <summary>
/// コンパイル時に Generated.BuildInfo 型を生成する、簡単な Source Generator です（記事 17 章）。
/// この Generator を参照したプロジェクトでは、ソースファイルを書いていなくても
/// Generated.BuildInfo.Tool を使えるようになります。
/// </summary>
[Generator]
public sealed class BuildInfoGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            var source = """
namespace Generated;

public static class BuildInfo
{
    public static string Tool => "Roslyn Source Generator";
}
""";

            ctx.AddSource(
                "BuildInfo.g.cs",
                SourceText.From(source, Encoding.UTF8));
        });
    }
}
