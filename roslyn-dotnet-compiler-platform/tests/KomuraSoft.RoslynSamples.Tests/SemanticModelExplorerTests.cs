using Xunit;

namespace KomuraSoft.RoslynSamples.Tests;

public class SemanticModelExplorerTests
{
    [Fact]
    public void ResolveFirstInvocation_ResolvesConsoleWriteLine()
    {
        // 記事 8 章のサンプルコード
        var source = """
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello");
    }
}
""";

        var resolved = SemanticModelExplorer.ResolveFirstInvocation(source);

        Assert.NotNull(resolved);
        Assert.Equal("System.Console", resolved.ContainingType);
        Assert.Equal("WriteLine", resolved.MethodName);
    }

    [Fact]
    public void ResolveFirstInvocation_ResolvesAliasedConsole()
    {
        // using alias を使っても、意味解析では同じ System.Console に解決される（記事 9・22 章）
        var source = """
using C = System.Console;

class Program
{
    static void Main()
    {
        C.WriteLine("Hello");
    }
}
""";

        var resolved = SemanticModelExplorer.ResolveFirstInvocation(source);

        Assert.NotNull(resolved);
        Assert.Equal("System.Console", resolved.ContainingType);
        Assert.Equal("WriteLine", resolved.MethodName);
    }

    [Fact]
    public void ResolveFirstInvocation_DistinguishesUserDefinedConsole()
    {
        // 見た目は Console.WriteLine でも、別の型なら別のシンボルに解決される（記事 1 章）
        var source = """
using Console = MyCompany.Logging.Console;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello");
    }
}

namespace MyCompany.Logging
{
    public static class Console
    {
        public static void WriteLine(string message) { }
    }
}
""";

        var resolved = SemanticModelExplorer.ResolveFirstInvocation(source);

        Assert.NotNull(resolved);
        Assert.Equal("MyCompany.Logging.Console", resolved.ContainingType);
        Assert.Equal("WriteLine", resolved.MethodName);
    }
}
