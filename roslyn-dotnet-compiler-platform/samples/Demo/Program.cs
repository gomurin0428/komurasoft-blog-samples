using KomuraSoft.RoslynSamples;
using KomuraSoft.RoslynSamples.Demo;

// Roslyn（.NET Compiler Platform）の基本操作を実演するデモです。
// 引数なし: Syntax Tree / 不変な構文木 / SemanticModel / Source Generator を実演します。
// 引数あり: 指定した .sln を MSBuildWorkspace で開き、ソリューション全体を解析します。
//   例: dotnet run --project samples/Demo -- RoslynCompilerPlatform.sln

if (args.Length > 0)
{
    await SolutionAnalysisDemo.RunAsync(args[0]);
    return;
}

// 1. Syntax Tree: 文字列検索ではなく、C# の構文として「メソッド宣言」を探す（記事 5 章）
Console.WriteLine("=== 1. Syntax Tree: メソッド宣言を探す（記事 5 章） ===");

var source = """
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

foreach (var methodName in SyntaxTreeExplorer.GetMethodNames(source))
{
    Console.WriteLine($"メソッド宣言: {methodName}");
}

// 2. Syntax Tree は不変: 変更を加えた新しい構文木を作る（記事 7 章）
Console.WriteLine();
Console.WriteLine("=== 2. 不変な構文木: メソッド名を変更した新しい木を作る（記事 7 章） ===");

var renamed = MethodRenamer.RenameMethod(source, oldName: "Rename", newName: "ChangeName");
Console.WriteLine(renamed);
Console.WriteLine("(コメントや空白などの Trivia が維持されている点に注目)");

// 3. SemanticModel: 構文ノードがどのシンボルに解決されたかを取得する（記事 8 章）
Console.WriteLine();
Console.WriteLine("=== 3. SemanticModel: Console.WriteLine の解決先を調べる（記事 8 章） ===");

var helloSource = """
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello");
    }
}
""";

var resolved = SemanticModelExplorer.ResolveFirstInvocation(helloSource);
Console.WriteLine($"ContainingType: {resolved?.ContainingType}");
Console.WriteLine($"MethodName    : {resolved?.MethodName}");

// 4. Source Generator: コンパイル時に生成された型を使う（記事 17・18 章）
// Generated.BuildInfo は、このプロジェクトのソースには存在せず、
// BuildInfoGenerator がコンパイル時に生成しています。
Console.WriteLine();
Console.WriteLine("=== 4. Source Generator が生成した型を使う（記事 17・18 章） ===");
Console.WriteLine(Generated.BuildInfo.Tool);

Console.WriteLine();
Console.WriteLine("[demo] done");
