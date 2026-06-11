using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KomuraSoft.RoslynSamples;

/// <summary>
/// ソースコードを Syntax Tree として読み、構文要素を探します（記事 5 章）。
/// 文字列検索で void を探すのではなく、C# の構文として「メソッド宣言」を探します。
/// </summary>
public static class SyntaxTreeExplorer
{
    /// <summary>
    /// ソースコードに含まれるメソッド宣言の名前を、出現順に列挙します。
    /// </summary>
    public static IReadOnlyList<string> GetMethodNames(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();

        var methods = root
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        return methods
            .Select(method => method.Identifier.Text)
            .ToArray();
    }
}
