using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace KomuraSoft.RoslynSamples.Demo;

/// <summary>
/// MSBuildWorkspace でソリューション全体を読み込み、調査ツールとして解析します（記事 11・21・36 章）。
/// </summary>
public static class SolutionAnalysisDemo
{
    public static async Task RunAsync(string solutionPath)
    {
        MSBuildLocator.RegisterDefaults();

        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath);

        Console.WriteLine("=== ソリューション内のドキュメント（記事 11 章） ===");
        await ListDocumentsAsync(solution);

        Console.WriteLine();
        Console.WriteLine("=== public class の一覧（記事 21 章） ===");
        await ListPublicClassesAsync(solution);

        Console.WriteLine();
        Console.WriteLine("=== メソッド呼び出しの一覧（記事 36 章） ===");
        await ListMethodCallsAsync(solution);
    }

    /// <summary>ソリューション全体から全プロジェクトを読み込み、全ドキュメントを解析する（記事 11 章）。</summary>
    private static async Task ListDocumentsAsync(Solution solution)
    {
        foreach (var project in solution.Projects)
        {
            Console.WriteLine(project.Name);

            foreach (var document in project.Documents)
            {
                var root = await document.GetSyntaxRootAsync();
                Console.WriteLine($"  {document.Name}: {root?.DescendantNodes().Count()} nodes");
            }
        }
    }

    /// <summary>ソリューション内の public class を列挙する（記事 21 章）。構文だけを見ている例。</summary>
    private static async Task ListPublicClassesAsync(Solution solution)
    {
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                var root = await document.GetSyntaxRootAsync();
                if (root is null)
                {
                    continue;
                }

                var classes = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(c => c.Modifiers.Any(m => m.Text == "public"));

                foreach (var cls in classes)
                {
                    Console.WriteLine($"{project.Name},{document.FilePath},{cls.Identifier.Text}");
                }
            }
        }
    }

    /// <summary>ソリューション内のメソッド呼び出しを一覧化する（記事 36 章）。SemanticModel を使う例。</summary>
    private static async Task ListMethodCallsAsync(Solution solution)
    {
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation is null)
            {
                continue;
            }

            foreach (var document in project.Documents)
            {
                var tree = await document.GetSyntaxTreeAsync();
                if (tree is null)
                {
                    continue;
                }

                var root = await tree.GetRootAsync();
                var semanticModel = compilation.GetSemanticModel(tree);

                var invocations = root
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>();

                foreach (var invocation in invocations)
                {
                    var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    if (symbol is null)
                    {
                        continue;
                    }

                    var lineSpan = invocation.GetLocation().GetLineSpan();
                    var line = lineSpan.StartLinePosition.Line + 1;

                    Console.WriteLine(string.Join(",", new[]
                    {
                        project.Name,
                        document.FilePath ?? document.Name,
                        line.ToString(),
                        symbol.ContainingType.ToDisplayString(),
                        symbol.Name
                    }));
                }
            }
        }
    }
}
