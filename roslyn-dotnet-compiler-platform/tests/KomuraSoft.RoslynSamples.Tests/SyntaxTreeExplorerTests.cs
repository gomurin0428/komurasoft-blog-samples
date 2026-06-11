using Xunit;

namespace KomuraSoft.RoslynSamples.Tests;

public class SyntaxTreeExplorerTests
{
    [Fact]
    public void GetMethodNames_FindsMethodDeclaration()
    {
        // 記事 5 章のサンプルコード。プロパティのアクセサーは
        // MethodDeclarationSyntax ではないため、Rename だけが見つかる。
        var source = """
class User
{
    public string Name { get; set; }

    public void Rename(string name)
    {
        Name = name;
    }
}
""";

        var methods = SyntaxTreeExplorer.GetMethodNames(source);

        Assert.Equal(["Rename"], methods);
    }

    [Fact]
    public void GetMethodNames_ReturnsMethodsInSourceOrder()
    {
        var source = """
class Calculator
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;

    private static int Twice(int value) => value * 2;
}
""";

        var methods = SyntaxTreeExplorer.GetMethodNames(source);

        Assert.Equal(["Add", "Subtract", "Twice"], methods);
    }

    [Fact]
    public void GetMethodNames_IgnoresMethodNamesInsideStringsAndComments()
    {
        // 文字列リテラルやコメントの中の「メソッドらしき文字列」は拾わない（記事 22 章）
        var source = """
class C
{
    // void NotAMethod() { }
    private string text = "void AlsoNotAMethod() { }";

    public void RealMethod() { }
}
""";

        var methods = SyntaxTreeExplorer.GetMethodNames(source);

        Assert.Equal(["RealMethod"], methods);
    }
}
