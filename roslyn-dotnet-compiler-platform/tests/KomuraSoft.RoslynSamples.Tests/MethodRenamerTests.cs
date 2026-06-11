using Xunit;

namespace KomuraSoft.RoslynSamples.Tests;

public class MethodRenamerTests
{
    private const string Source = """
class User
{
    // ユーザーの表示名
    public string Name { get; set; }

    public void Rename(string name)
    {
        Name = name;
    }
}
""";

    [Fact]
    public void RenameMethod_ProducesNewSourceWithNewName()
    {
        var renamed = MethodRenamer.RenameMethod(Source, "Rename", "ChangeName");

        Assert.Contains("public void ChangeName(string name)", renamed);
        Assert.DoesNotContain("Rename(", renamed);
    }

    [Fact]
    public void RenameMethod_PreservesCommentsAndWhitespace()
    {
        // Trivia（コメント・改行）が維持されることを確認する（記事 6・7 章）
        var renamed = MethodRenamer.RenameMethod(Source, "Rename", "ChangeName");

        Assert.Contains("// ユーザーの表示名", renamed);
        Assert.Contains("    public void ChangeName(string name)", renamed);
    }

    [Fact]
    public void RenameMethod_DoesNotMutateOriginalSource()
    {
        // 構文木は不変なので、元のソースから作り直しても結果は変わらない
        var first = MethodRenamer.RenameMethod(Source, "Rename", "ChangeName");
        var second = MethodRenamer.RenameMethod(Source, "Rename", "ChangeName");

        Assert.Equal(first, second);
    }
}
