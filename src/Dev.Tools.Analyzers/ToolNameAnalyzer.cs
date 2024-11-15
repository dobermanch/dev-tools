using System.Collections.Immutable;
using Dev.Tools.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Dev.Tools.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ToolNameAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor NotUniqueRule = new(
        "DT1001",
        "ToolDefinitionAttribute name should be unique",
        "The name '{0}' is already used in another ToolDefinitionAttribute",
        "Naming",
        DiagnosticSeverity.Error,
        description: "ToolDefinitionAttribute name should be unique across the project.",
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public static readonly DiagnosticDescriptor NotValidRule = new(
        "DT1002",
        "ToolDefinitionAttribute name is not valid",
        "The name '{0}' not valid. It should contain only letters, numbers, and the '-' character, start with a letter, and be no longer than 20 characters.",
        "Naming",
        DiagnosticSeverity.Error,
        description:
        "ToolDefinitionAttribute name should be valid. It should contain only letters, numbers, and the '-' character, start with a letter, and be no longer than 20 characters.",
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [NotUniqueRule, NotValidRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var attributeList = new List<(string name, Location location)>();

            compilationContext.RegisterSyntaxNodeAction(syntaxContext =>
            {
                var attributeSyntax = (AttributeSyntax)syntaxContext.Node;
                var symbolInfo = syntaxContext.SemanticModel.GetSymbolInfo(attributeSyntax);
                if (symbolInfo.Symbol?.ContainingType is not { Name: nameof(ToolDefinitionAttribute) })
                {
                    return;
                }

                LiteralExpressionSyntax? literal =
                    attributeSyntax
                        .ArgumentList
                        ?.Arguments
                        .Where(arg => arg.NameEquals?.Name.Identifier.Text == nameof(ToolDefinitionAttribute.Name))
                        .Select(it => it.Expression)
                        .OfType<LiteralExpressionSyntax>()
                        .FirstOrDefault();
                if (literal is null)
                {
                    return;
                }

                var name = literal.Token.ValueText;
                if (CheckIfNameIsValid(name))
                {
                    attributeList.Add((name, literal.GetLocation()));
                }
                else
                {
                    syntaxContext.ReportDiagnostic(Diagnostic.Create(
                        NotValidRule,
                        literal.GetLocation(),
                        name)
                    );
                }
            }, SyntaxKind.Attribute);

            compilationContext.RegisterCompilationEndAction(compilationEndContext =>
            {
                PerformUniquenessCheck(attributeList, compilationEndContext);
            });
        });
    }

    private static void PerformUniquenessCheck(List<(string name, Location location)> attributeList,
        CompilationAnalysisContext compilationEndContext)
    {
        var duplicateNames = attributeList
            .GroupBy(attr => attr.name)
            .Where(it => it.Count() > 1)
            .SelectMany(it => it);

        foreach (var duplicate in duplicateNames)
        {
            compilationEndContext.ReportDiagnostic(Diagnostic.Create(
                NotUniqueRule,
                duplicate.location,
                duplicate.name)
            );
        }
    }

    private bool CheckIfNameIsValid(string name)
    {
        const uint nameMaxLength = 20;
        return !string.IsNullOrEmpty(name)
               && char.IsLetter(name[0])
               && name.All(it => char.IsLetterOrDigit(it) || it is '-')
               && name.Length <= nameMaxLength;
    }
}