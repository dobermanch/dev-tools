using System.Collections.Concurrent;
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

    public static readonly DiagnosticDescriptor MissingDescriptionRule = new(
        "DT1004",
        "A tool must describe itself",
        "The tool '{0}' must set a non-empty Description on ToolDefinitionAttribute",
        "Documentation",
        DiagnosticSeverity.Error,
        description: "Tool text is authored in place and shipped as metadata to every frontend, "
                     + "so a tool without a description is invisible to CLI help, the API schema and MCP clients.",
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultiLineTitleRule = new(
        "DT1005",
        "A tool title must be a single line",
        "The Title of tool '{0}' must be a single line; put the detail in Description instead",
        "Documentation",
        DiagnosticSeverity.Error,
        description: "Titles head one-line lists such as the CLI command list and the Web tool cards. "
                     + "Only Description may span several lines.",
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingPropertyDescriptionRule = new(
        "DT1006",
        "Tool arguments and results must be described",
        "The '{0}' property of '{1}' must carry a [Description] attribute",
        "Documentation",
        DiagnosticSeverity.Error,
        description: "Every argument and result property reaches users as CLI help, an OpenAPI schema "
                     + "and an MCP input schema, so each one needs English text where it is declared.",
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingEnumValueDescriptionRule = new(
        "DT1007",
        "Tool enum values must be described",
        "The '{0}' value of enum '{1}' must carry a [Description] attribute",
        "Documentation",
        DiagnosticSeverity.Error,
        description: "Enum values appear as the allowed choices in CLI help and in the API and MCP "
                     + "schemas, where a bare member name rarely explains what the choice does.",
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        =>
        [
            NotUniqueRule,
            NotValidRule,
            NotPublicRule,
            MissingDescriptionRule,
            MultiLineTitleRule,
            MissingPropertyDescriptionRule,
            MissingEnumValueDescriptionRule
        ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Syntax node actions run concurrently (EnableConcurrentExecution),
            // so the collection they feed has to be thread-safe.
            var attributeList = new ConcurrentBag<(string name, Location location)>();

            compilationContext.RegisterSyntaxNodeAction(syntaxContext =>
            {
                var (syntax, symbol) = syntaxContext.Node.GetTypeNode(syntaxContext.SemanticModel);
                if (syntax is null || symbol is null)
                {
                    return;
                }

                var attribute = syntaxContext.Node.GetAttributeSyntax(
                    syntaxContext.SemanticModel,
                    CodeDefinitions.ToolDefinitionAttribute.TypeFullName);

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
                CheckToolText(symbol, attribute!, syntaxContext);
                CheckDescribedMembers(symbol, syntaxContext);

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

    /// <summary>
    /// A tool has to describe itself, and its one-line Title has to stay one line.
    /// </summary>
    private static void CheckToolText(
        INamedTypeSymbol symbol,
        AttributeSyntax attribute,
        SyntaxNodeAnalysisContext syntaxContext)
    {
        var attributeData = symbol
            .GetAttributes()
            .FirstOrDefault(it => it.AttributeClass?.ToDisplayString()
                                  == CodeDefinitions.ToolDefinitionAttribute.TypeFullName);
        if (attributeData is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(GetNamedArgument(attributeData, "Description")))
        {
            syntaxContext.ReportDiagnostic(Diagnostic.Create(
                MissingDescriptionRule,
                ArgumentLocation(attribute, "Description") ?? attribute.GetLocation(),
                symbol.Name));
        }

        var title = GetNamedArgument(attributeData, "Title");
        if (title is not null && (title.Contains("\n") || title.Contains("\r")))
        {
            syntaxContext.ReportDiagnostic(Diagnostic.Create(
                MultiLineTitleRule,
                ArgumentLocation(attribute, "Title") ?? attribute.GetLocation(),
                symbol.Name));
        }
    }

    /// <summary>
    /// Every argument and result property, and every enum they use, needs English text in place.
    /// The types are found the same way the generators find them: the two arguments of the tool
    /// interface, so nested helper types the catalog never reads are left alone.
    /// </summary>
    private static void CheckDescribedMembers(INamedTypeSymbol symbol, SyntaxNodeAnalysisContext syntaxContext)
    {
        foreach (var toolType in FindToolTypeArguments(symbol))
        {
            var properties = toolType
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(it => it.DeclaredAccessibility == Accessibility.Public)
                .ToArray();

            foreach (var property in properties)
            {
                if (!HasDescription(property) && SourceLocation(property) is { } propertyLocation)
                {
                    syntaxContext.ReportDiagnostic(Diagnostic.Create(
                        MissingPropertyDescriptionRule,
                        propertyLocation,
                        property.Name,
                        toolType.Name));
                }

                if (property.Type is not INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType
                    || IsCoreEnum(enumType))
                {
                    continue;
                }

                foreach (var value in enumType.GetMembers().OfType<IFieldSymbol>().Where(it => it.HasConstantValue))
                {
                    if (!HasDescription(value) && SourceLocation(value) is { } valueLocation)
                    {
                        syntaxContext.ReportDiagnostic(Diagnostic.Create(
                            MissingEnumValueDescriptionRule,
                            valueLocation,
                            value.Name,
                            enumType.Name));
                    }
                }
            }
        }
    }

    private static ITypeSymbol[] FindToolTypeArguments(INamedTypeSymbol? typeSymbol)
    {
        while (typeSymbol != null)
        {
            var toolInterface = typeSymbol
                .Interfaces
                .FirstOrDefault(it => it.IsGenericType && it is { TypeArguments.Length: 2 });
            if (toolInterface != null)
            {
                return [.. toolInterface.TypeArguments];
            }

            typeSymbol = typeSymbol.BaseType;
        }

        return [];
    }

    private static bool IsCoreEnum(INamedTypeSymbol enumType)
        => enumType.ContainingNamespace.ToDisplayString() == CodeDefinitions.ToolsGlobalNamespace
           && enumType.Name is "Category" or "Keyword" or "ErrorCode";

    private static bool HasDescription(ISymbol symbol)
        => symbol
            .GetAttributes()
            .Any(it => it.AttributeClass?.ToDisplayString() == "System.ComponentModel.DescriptionAttribute"
                       && !string.IsNullOrWhiteSpace(it.ConstructorArguments.FirstOrDefault().Value?.ToString()));

    private static Location? SourceLocation(ISymbol symbol)
        => symbol.Locations.FirstOrDefault(it => it.IsInSource);

    private static string? GetNamedArgument(AttributeData attributeData, string name)
        => attributeData.NamedArguments
            .FirstOrDefault(it => it.Key == name)
            .Value.Value as string;

    private static Location? ArgumentLocation(AttributeSyntax attribute, string name)
        => attribute
            .ArgumentList
            ?.Arguments
            .FirstOrDefault(it => it.NameEquals?.Name.Identifier.Text == name)
            ?.GetLocation();

    private static void CheckNameIsUnique(ConcurrentBag<(string name, Location location)> attributeList,
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