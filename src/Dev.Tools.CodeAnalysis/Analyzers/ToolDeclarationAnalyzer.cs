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

    public static readonly DiagnosticDescriptor NotAToolRule = new(
        "DT1008",
        "A tool must implement ITool<TArgs, TResult>",
        "The '{0}' class is marked with ToolDefinitionAttribute but does not implement ITool<TArgs, TResult>",
        "Declaration",
        DiagnosticSeverity.Error,
        description: "The generators read a tool's argument and result types from its ITool<,> interface. "
                     + "A class without one is skipped, so it produces no CLI command, no API endpoint "
                     + "and no MCP tool - previously without saying so.",
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AmbiguousPipeInputRule = new(
        "DT1009",
        "A tool may pipe at most one argument",
        "'{0}' marks {1} properties with [PipeInput]; at most one argument can be the piped one",
        "Declaration",
        DiagnosticSeverity.Error,
        description: "[PipeInput] names the single argument the CLI fills from stdin and passes "
                     + "positionally. Several would leave which one wins up to declaration order.",
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AmbiguousPipeOutputRule = new(
        "DT1010",
        "A tool may pipe at most one result",
        "'{0}' marks {1} properties with [PipeOutput]; at most one result can be the piped one",
        "Declaration",
        DiagnosticSeverity.Error,
        description: "[PipeOutput] names the single value the CLI writes to stdout so tools can be "
                     + "chained. Several would leave which one wins up to declaration order.",
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AmbiguousArgsConstructorRule = new(
        "DT1011",
        "Tool arguments must have one public constructor",
        "'{0}' has {1} public constructors; the generators read argument defaults from one of them",
        "Declaration",
        DiagnosticSeverity.Error,
        description: "Default values reach CLI options and MCP parameters from the Args constructor. "
                     + "With more than one the generators take whichever comes first, so the defaults a "
                     + "user sees depend on declaration order.",
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedArgumentTypeRule = new(
        "DT1012",
        "Tool argument types must be bindable by every frontend",
        "The '{0}' argument of '{1}' is a {2}, which the CLI, API and MCP frontends cannot bind",
        "Declaration",
        DiagnosticSeverity.Warning,
        description: "An argument becomes a Spectre command option, an OpenAPI schema property and an "
                     + "MCP parameter. Types outside the supported set fail late and obscurely - as a "
                     + "compile error inside generated code, or as a binding failure at run time.",
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
            MissingEnumValueDescriptionRule,
            NotAToolRule,
            AmbiguousPipeInputRule,
            AmbiguousPipeOutputRule,
            AmbiguousArgsConstructorRule,
            UnsupportedArgumentTypeRule
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
                CheckToolShape(type, symbol, syntaxContext);

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
    /// Everything the generators assume about a tool's shape. The argument and result types are found
    /// the way the generators find them - the two type arguments of ITool&lt;,&gt; - so nested helper
    /// types nothing reads are left alone.
    /// </summary>
    private static void CheckToolShape(
        TypeDeclaration type,
        INamedTypeSymbol symbol,
        SyntaxNodeAnalysisContext syntaxContext)
    {
        var toolInterface = FindToolInterface(symbol);
        if (toolInterface is null)
        {
            syntaxContext.ReportDiagnostic(Diagnostic.Create(NotAToolRule, type.Location, type.TypeName));
            return;
        }

        var args = toolInterface.TypeArguments[0];
        var result = toolInterface.TypeArguments[1];

        CheckDescribedMembers(args, syntaxContext);
        CheckDescribedMembers(result, syntaxContext);

        CheckAtMostOneMarker(args, "PipeInputAttribute", AmbiguousPipeInputRule, syntaxContext);
        CheckAtMostOneMarker(result, "PipeOutputAttribute", AmbiguousPipeOutputRule, syntaxContext);

        CheckSingleConstructor(args, syntaxContext);
        CheckArgumentTypes(args, syntaxContext);
    }

    /// <summary>
    /// Every property of the type, and every enum they use, needs English text in place.
    /// </summary>
    private static void CheckDescribedMembers(ITypeSymbol toolType, SyntaxNodeAnalysisContext syntaxContext)
    {
        foreach (var property in PublicProperties(toolType))
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

    /// <summary>
    /// [PipeInput] and [PipeOutput] each name one property, and the frontends take the first they
    /// find. A tool may carry neither - the generators take no input, the parsers have no single
    /// output worth piping - but not two.
    /// </summary>
    private static void CheckAtMostOneMarker(
        ITypeSymbol toolType,
        string attributeName,
        DiagnosticDescriptor rule,
        SyntaxNodeAnalysisContext syntaxContext)
    {
        var marked = PublicProperties(toolType)
            .Where(it => it.GetAttributes().Any(a => a.AttributeClass?.Name == attributeName))
            .ToArray();
        if (marked.Length <= 1)
        {
            return;
        }

        foreach (var property in marked.Where(it => SourceLocation(it) is not null))
        {
            syntaxContext.ReportDiagnostic(Diagnostic.Create(
                rule,
                SourceLocation(property),
                toolType.Name,
                marked.Length));
        }
    }

    private static void CheckSingleConstructor(ITypeSymbol toolType, SyntaxNodeAnalysisContext syntaxContext)
    {
        if (toolType is not INamedTypeSymbol namedType)
        {
            return;
        }

        var constructors = namedType.Constructors
            .Where(it => it.DeclaredAccessibility == Accessibility.Public && !it.IsImplicitlyDeclared)
            .ToArray();
        if (constructors.Length <= 1)
        {
            return;
        }

        foreach (var constructor in constructors.Where(it => SourceLocation(it) is not null))
        {
            syntaxContext.ReportDiagnostic(Diagnostic.Create(
                AmbiguousArgsConstructorRule,
                SourceLocation(constructor),
                toolType.Name,
                constructors.Length));
        }
    }

    private static void CheckArgumentTypes(ITypeSymbol toolType, SyntaxNodeAnalysisContext syntaxContext)
    {
        foreach (var property in PublicProperties(toolType))
        {
            if (IsBindableArgumentType(property.Type) || SourceLocation(property) is not { } location)
            {
                continue;
            }

            syntaxContext.ReportDiagnostic(Diagnostic.Create(
                UnsupportedArgumentTypeRule,
                location,
                property.Name,
                toolType.Name,
                property.Type.ToDisplayString()));
        }
    }

    /// <summary>
    /// The types a Spectre option, an OpenAPI schema property and an MCP parameter can all carry:
    /// the scalars every one of them converts, plus a sequence of those.
    /// </summary>
    private static bool IsBindableArgumentType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } nullable
            && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return IsBindableArgumentType(nullable.TypeArguments[0]);
        }

        if (type is IArrayTypeSymbol array)
        {
            return IsBindableScalar(array.ElementType);
        }

        if (type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } sequence
            && SupportedSequences.Contains(sequence.ConstructedFrom.ToDisplayString()))
        {
            return IsBindableScalar(sequence.TypeArguments[0]);
        }

        return IsBindableScalar(type);
    }

    private static bool IsBindableScalar(ITypeSymbol type)
        => type.TypeKind == TypeKind.Enum
           || type.SpecialType is SpecialType.System_String
               or SpecialType.System_Boolean
               or SpecialType.System_Char
               or SpecialType.System_SByte
               or SpecialType.System_Byte
               or SpecialType.System_Int16
               or SpecialType.System_UInt16
               or SpecialType.System_Int32
               or SpecialType.System_UInt32
               or SpecialType.System_Int64
               or SpecialType.System_UInt64
               or SpecialType.System_Single
               or SpecialType.System_Double
               or SpecialType.System_Decimal
               or SpecialType.System_DateTime
           || SupportedScalars.Contains(type.ToDisplayString().TrimEnd('?'));

    private static readonly HashSet<string> SupportedScalars =
    [
        "System.Guid",
        "System.DateTimeOffset",
        "System.TimeSpan",
        "System.DateOnly",
        "System.TimeOnly",
        "System.Uri"
    ];

    private static readonly HashSet<string> SupportedSequences =
    [
        "System.Collections.Generic.IList<T>",
        "System.Collections.Generic.List<T>",
        "System.Collections.Generic.ICollection<T>",
        "System.Collections.Generic.IEnumerable<T>",
        "System.Collections.Generic.IReadOnlyList<T>",
        "System.Collections.Generic.IReadOnlyCollection<T>"
    ];

    private static IPropertySymbol[] PublicProperties(ITypeSymbol toolType)
        => toolType
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(it => it.DeclaredAccessibility == Accessibility.Public)
            .ToArray();

    /// <summary>
    /// The tool's ITool&lt;TArgs, TResult&gt;, matched by identity rather than by shape - the
    /// generators used to take any generic interface with two type arguments.
    /// </summary>
    private static INamedTypeSymbol? FindToolInterface(INamedTypeSymbol symbol)
        => symbol.AllInterfaces.FirstOrDefault(it =>
            it.MetadataName == CodeDefinitions.ToolInterfaceMetadataName
            && it.ContainingNamespace.ToDisplayString() == CodeDefinitions.ToolsGlobalNamespace);

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