# Roslynとは何か ── サンプルコード

ブログ記事「[Roslynとは何か ── C#コードをコンパイラの視点で読む・直す・生成する](https://comcomponent.com/blog/2026/06/10/002-roslyn-dotnet-compiler-platform/)」のサンプルコードです。

Roslyn（.NET Compiler Platform）を使うと、C# のコードを文字列ではなく、コンパイラの理解した構造として扱えます。このサンプルでは、記事で紹介している **Syntax Tree**（構文）、**SemanticModel**（意味）、**Workspace**（ソリューション全体）、**Analyzer**、**Source Generator** の各要素を、ビルド・実行・テストできる形で提供します。

## 構成

```
roslyn-dotnet-compiler-platform/
├── src/KomuraSoft.RoslynSamples/             Roslyn API を使うライブラリ
│   ├── SyntaxTreeExplorer.cs                 Syntax Tree からメソッド宣言を探す（記事 5 章）
│   ├── MethodRenamer.cs                      不変な構文木から新しい木を作る（記事 7 章）
│   ├── SemanticModelExplorer.cs              呼び出しの解決先シンボルを調べる（記事 8 章）
│   └── AnalyzerRunner.cs                     Analyzer をソースに対して実行するヘルパー（記事 31 章）
├── src/KomuraSoft.RoslynSamples.Analyzers/   Analyzer / Source Generator（netstandard2.0）
│   ├── NoDateTimeNowAnalyzer.cs              DateTime.Now を警告する Analyzer（記事 15 章）
│   └── BuildInfoGenerator.cs                 Generated.BuildInfo を生成する Generator（記事 17 章）
├── samples/Demo/                             コンソールデモ
│   ├── Program.cs                            構文・意味・生成コードの実演（記事 5・7・8・17・18 章）
│   └── SolutionAnalysisDemo.cs               MSBuildWorkspace でソリューション全体を解析（記事 11・21・36 章）
└── tests/KomuraSoft.RoslynSamples.Tests/     誤検出・検出漏れの両方を確認するユニットテスト（記事 31 章）
```

## 必要環境

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 以降

## 実行方法

デモ（Syntax Tree の探索、構文木の書き換え、SemanticModel による名前解決、Source Generator が生成した型の利用を実演します）:

```console
dotnet run --project samples/Demo
```

ソリューション全体の解析（MSBuildWorkspace でソリューションを開き、public class とメソッド呼び出しを CSV 形式で一覧化します。このサンプル自身のソリューションを対象にできます）:

```console
dotnet run --project samples/Demo -- RoslynCompilerPlatform.sln
```

テスト（Analyzer の検出・誤検出、using alias や同名別型の扱い、Source Generator の生成結果などを検証します）:

```console
dotnet test
```

## ポイント

- C# は文字列ではない。`Console.WriteLine` という見た目が同じでも、名前解決の結果（シンボル）は別物になり得る
- 構文（どう書かれているか）は Syntax Tree、意味（何を指しているか）は SemanticModel、と役割が分かれている
- Syntax Tree は不変。書き換えは「変更を加えた新しい木を作る」形で行い、Trivia（コメント・空白）は維持される
- Analyzer は「構文で候補を絞り、SemanticModel で本当に対象か確認し、Diagnostic で報告する」流れで書く
- Analyzer のテストは、検出すべきコードだけでなく「検出してはいけないコード」（同名の別型など）も用意する
- Source Generator は既存コードを書き換えるものではなく、追加のソースコードを生成してコンパイルに参加させるもの
- Analyzer / Source Generator はコンパイラに読み込まれるため、プロジェクト参照では `OutputItemType="Analyzer"` と `ReferenceOutputAssembly="false"` を指定し、netstandard2.0 を対象にする
- 生成された `.g.cs` は `EmitCompilerGeneratedFiles` で出力して確認できる（`samples/Demo/obj/Generated/` 配下）

詳しい解説は[記事本文](https://comcomponent.com/blog/2026/06/10/002-roslyn-dotnet-compiler-platform/)をご覧ください。
