using KomuraSoft.RoslynSamples.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace KomuraSoft.RoslynSamples.Tests;

/// <summary>
/// Analyzer は「検出すべきコード」と「検出してはいけないコード」の両方をテストする（記事 31 章）。
/// </summary>
public class NoDateTimeNowAnalyzerTests
{
    private static Task<System.Collections.Immutable.ImmutableArray<Diagnostic>> RunAsync(string source)
        => AnalyzerRunner.RunAsync(new NoDateTimeNowAnalyzer(), source);

    [Fact]
    public async Task Reports_FullyQualifiedDateTimeNow()
    {
        // 検出すべき
        var source = """
class C
{
    void M()
    {
        var x = System.DateTime.Now;
    }
}
""";

        var diagnostics = await RunAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("CMP001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task Reports_DateTimeNow_WithUsing()
    {
        // using がある場合も検出すべき
        var source = """
using System;

class C
{
    void M()
    {
        var x = DateTime.Now;
    }
}
""";

        var diagnostics = await RunAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("CMP001", diagnostic.Id);
    }

    [Fact]
    public async Task Reports_DateTimeNow_WithUsingAlias()
    {
        // using alias を使っていても、意味解析で System.DateTime.Now と分かるので検出すべき
        var source = """
using D = System.DateTime;

class C
{
    void M()
    {
        var x = D.Now;
    }
}
""";

        var diagnostics = await RunAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("CMP001", diagnostic.Id);
    }

    [Fact]
    public async Task DoesNotReport_UserDefinedDateTimeNow()
    {
        // 別型なら検出してはいけない（文字列検索では間違えやすいケース）
        var source = """
namespace MyCompany
{
    public static class DateTime
    {
        public static string Now => "now";
    }

    class C
    {
        void M()
        {
            var x = DateTime.Now;
        }
    }
}
""";

        var diagnostics = await RunAsync(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReport_DateTimeOffsetUtcNow()
    {
        // 推奨される書き方は検出してはいけない
        var source = """
using System;

class C
{
    void M()
    {
        var x = DateTimeOffset.UtcNow;
    }
}
""";

        var diagnostics = await RunAsync(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DoesNotReport_OtherNowProperties()
    {
        // 名前が Now でも System.DateTime 以外なら検出してはいけない
        var source = """
using System;

class C
{
    void M()
    {
        var x = DateTimeOffset.Now;
    }
}
""";

        var diagnostics = await RunAsync(source);

        Assert.Empty(diagnostics);
    }
}
