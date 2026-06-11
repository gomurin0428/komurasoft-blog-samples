using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KomuraSoft.RoslynSamples;

/// <summary>
/// SemanticModel を使い、構文ノードがどのシンボルに解決されたかを取得します（記事 8 章）。
/// 構文だけでなく、コンパイラの名前解決結果を使える点が Roslyn の強みです。
/// </summary>
public static class SemanticModelExplorer
{
    /// <summary>名前解決されたメソッド呼び出し。</summary>
    public sealed record ResolvedInvocation(string ContainingType, string MethodName);

    /// <summary>
    /// ソースコード中の最初のメソッド呼び出しを意味解析し、
    /// 実際にどの型のどのメソッドとして解決されたかを返します。
    /// 解決できなかった場合は null を返します。
    /// </summary>
    public static ResolvedInvocation? ResolveFirstInvocation(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);

        var compilation = CSharpCompilation.Create(
            assemblyName: "Sample",
            syntaxTrees: new[] { tree },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            });

        var semanticModel = compilation.GetSemanticModel(tree);
        var root = tree.GetCompilationUnitRoot();

        var invocation = root
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First();

        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        var method = (IMethodSymbol?)symbolInfo.Symbol;

        if (method is null)
        {
            return null;
        }

        return new ResolvedInvocation(
            method.ContainingType.ToDisplayString(),
            method.Name);
    }
}
