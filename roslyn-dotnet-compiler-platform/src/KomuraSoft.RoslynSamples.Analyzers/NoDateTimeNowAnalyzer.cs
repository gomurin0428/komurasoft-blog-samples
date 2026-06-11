using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KomuraSoft.RoslynSamples.Analyzers;

/// <summary>
/// DateTime.Now の使用を警告する Analyzer の最小イメージです（記事 15 章）。
/// 文字列として DateTime.Now を探すのではなく、SemanticModel で
/// 実際に System.DateTime.Now を指しているかを確認します。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoDateTimeNowAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "CMP001",
        title: "DateTime.Nowを直接使わない",
        messageFormat: "DateTime.Nowではなく、用途に応じてDateTimeOffset.UtcNowなどを検討してください",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(
            AnalyzeMemberAccess,
            SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        if (memberAccess.Name.Identifier.Text != "Now")
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
        if (symbol is not IPropertySymbol propertySymbol)
        {
            return;
        }

        if (propertySymbol.Name == "Now" &&
            propertySymbol.ContainingType.ToDisplayString() == "System.DateTime")
        {
            var diagnostic = Diagnostic.Create(Rule, memberAccess.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
