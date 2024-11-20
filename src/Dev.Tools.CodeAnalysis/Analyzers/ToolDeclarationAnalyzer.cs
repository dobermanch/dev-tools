using System.Collections.Immutable;
using Dev.Tools.CodeAnalysis.Core;
using Dev.Tools.CodeAnalysis.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Dev.Tools.CodeAnalysis.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ToolDeclarationAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor NotUniqueRule = new(
        "DT1001",
        "The tool name should be unique",
        "The tool name '{0}' is already been used",
        "Naming",
        DiagnosticSeverity.Error,
        description: "The tool name should be unique across the project.",
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public static readonly DiagnosticDescriptor NotValidRule = new(
        "DT1002",
        "The tool name is not valid",
        "The tool name '{0}' is not valid. It should contain only letters, numbers, and the '-' character, start with a letter, and be no longer than 20 characters.",
        "Naming",
        DiagnosticSeverity.Error,
        description: "The tool name should be valid.",
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public static readonly DiagnosticDescriptor NotPublicRule = new(
        "DT1003",
        "Class marked with ToolDefinitionAttribute should be public and sealed",
        "The '{0}' class marked with ToolDefinitionAttribute attribute should be public and sealed",
        "Declaration",
        DiagnosticSeverity.Error,
        description: "Class should be public and sealed.",
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [NotUniqueRule, NotValidRule, NotPublicRule];

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
                var (syntax, symbol) = syntaxContext.Node.GetTypeNode(syntaxContext.SemanticModel);
                if (syntax is null || symbol is null)
                {
                    return;
                }

                var attribute = syntaxContext.Node.GetAttributeSyntax(
                    syntaxContext.SemanticModel,
                    ToolsDefinitionGenerator.Attribute.TypeFullName);

                LiteralExpressionSyntax? literal =
                    attribute
                        ?.ArgumentList
                        ?.Arguments
                        .Where(arg => arg.NameEquals?.Name.Identifier.Text == "Name")
                        .Select(it => it.Expression)
                        .OfType<LiteralExpressionSyntax>()
                        .FirstOrDefault();
                if (literal is null)
                {
                    return;
                }

                var type = new TypeDeclaration
                {
                    Syntax = syntax,
                    Symbol = symbol
                };

                CheckClassDeclaration(type, syntaxContext);

                var name = literal.Token.ValueText;
                if (IsNameValid(name))
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
            }, SyntaxKind.ClassDeclaration);

            compilationContext.RegisterCompilationEndAction(compilationEndContext =>
            {
                CheckNameIsUnique(attributeList, compilationEndContext);
            });
        });
    }

    private static void CheckClassDeclaration(TypeDeclaration type, SyntaxNodeAnalysisContext syntaxContext)
    {
        if (type is { IsPublic: true, IsSealed: true })
        {
            return;
        }

        syntaxContext.ReportDiagnostic(
            Diagnostic.Create(NotPublicRule,
                type.Location,
                type.TypeName));
    }

    private static void CheckNameIsUnique(List<(string name, Location location)> attributeList,
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

    private bool IsNameValid(string name)
    {
        const uint nameMaxLength = 20;
        return !string.IsNullOrEmpty(name)
               && char.IsLetter(name[0])
               && name.All(it => char.IsLetterOrDigit(it) || it is '-')
               && name.Length <= nameMaxLength;
    }
}