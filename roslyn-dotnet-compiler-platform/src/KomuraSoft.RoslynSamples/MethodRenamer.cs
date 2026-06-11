using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KomuraSoft.RoslynSamples;

/// <summary>
/// 不変な Syntax Tree から、変更を加えた新しい構文木を作ります（記事 7 章）。
/// 既存の MethodDeclarationSyntax をその場で変更するのではなく、新しいノードを作ります。
/// </summary>
public static class MethodRenamer
{
    /// <summary>
    /// 指定した名前のメソッド宣言の名前を変更した、新しいソースコードを返します。
    /// Trivia（コメント・空白・改行）は維持されます。
    /// </summary>
    public static string RenameMethod(string source, string oldName, string newName)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();

        var oldMethod = root
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First(method => method.Identifier.Text == oldName);

        // 既存ノードは不変なので、変更を加えた新しいノードを作る
        var newMethod = oldMethod.WithIdentifier(
            SyntaxFactory.Identifier(newName));

        var newRoot = root.ReplaceNode(oldMethod, newMethod);

        return newRoot.ToFullString();
    }
}
