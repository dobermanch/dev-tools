using System.Collections.Immutable;
using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.CodeAnalysis.Generators;

/// <summary>
/// The incremental pipeline every generator runs, in one place.
///
/// The three frontend generators each spelled this out themselves, character for character apart
/// from the assembly attribute they guard on, so a frontend generator now says only which attribute
/// switches it on and what to emit.
///
/// Nothing that reaches a pipeline step here is a Roslyn object: symbols are read inside
/// <see cref="ToolModelFactory"/> and the guard attribute is reduced to a bool before it is
/// combined. That is what lets Roslyn compare one run's output with the next and skip the work.
/// </summary>
public static class ToolPipeline
{
    /// <summary>
    /// Names the pipeline steps so the caching tests can find them. Not a static class: the test
    /// helper takes it as a type argument and reads its constants by reflection.
    /// </summary>
    public sealed class TrackingNames
    {
        public const string Extract = nameof(Extract);
        public const string Collect = nameof(Collect);
    }

    public static readonly DiagnosticDescriptor ToolSkippedRule = new(
        "DTG1001",
        "A tool was skipped by the generators",
        "The class '{0}' is not usable as a tool and was skipped because {1}",
        "Generator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A class the generators cannot read produces no CLI command, API endpoint and no "
                     + "MCP tool. This used to happen silently - the tool simply did not appear.");

    public static readonly DiagnosticDescriptor NoToolsFoundRule = new(
        "DTG1002",
        "No tools were found",
        "The {0} generator is switched on for this assembly but found no tools to generate from",
        "Generator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A frontend generator reads tool classes from the referenced Dev.Tools assembly. "
                     + "Finding none usually means the reference is missing.");

    /// <summary>
    /// Registers a frontend generator: tools are read from the referenced Dev.Tools assembly, and
    /// nothing is emitted unless the consuming assembly carries <typeparamref name="TGuardAttribute"/>.
    /// </summary>
    public static void RegisterFrontend<TGuardAttribute>(
        IncrementalGeneratorInitializationContext context,
        string generatorName,
        Action<SourceProductionContext, ImmutableArray<ToolModel>> generate)
    {
        var tools = context.CompilationProvider
            .SelectMany((compilation, _) => compilation
                .References
                .Select(compilation.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .Where(it => it.Name == CodeDefinitions.ToolsAssemblyName)
                .Select(it => it.GlobalNamespace)
                .SelectMany(it => it.GetAllTypes(type =>
                    type.HasAttribute(CodeDefinitions.ToolDefinitionAttribute.TypeFullName)))
                .OfType<INamedTypeSymbol>()
                .Select(ToolModelFactory.Create))
            .WithTrackingName(TrackingNames.Extract)
            .Collect()
            .Combine(IsGeneratorEnabled<TGuardAttribute>(context))
            .WithTrackingName(TrackingNames.Collect);

        context.RegisterSourceOutput(tools, (spc, data) =>
        {
            if (!data.Right)
            {
                return;
            }

            Emit(spc, generatorName, data.Left, generate, warnWhenEmpty: true);
        });
    }

    /// <summary>
    /// Registers a generator that reads the tools of the assembly being compiled, from its syntax
    /// rather than from a metadata reference. No guard attribute: the library always wants its catalog.
    /// </summary>
    public static void RegisterSourceTools(
        IncrementalGeneratorInitializationContext context,
        string generatorName,
        Action<SourceProductionContext, ImmutableArray<ToolModel>> generate)
    {
        var tools = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                CodeDefinitions.ToolDefinitionAttribute.TypeFullName,
                predicate: (node, _) => node is ClassDeclarationSyntax declaration
                                        && declaration.HasAttribute(
                                            CodeDefinitions.ToolDefinitionAttribute.SyntaxTypeName),
                transform: (ctx, _) => ToolModelFactory.Create((INamedTypeSymbol)ctx.TargetSymbol))
            .WithTrackingName(TrackingNames.Extract)
            .Collect()
            .WithTrackingName(TrackingNames.Collect);

        context.RegisterSourceOutput(
            tools,
            (spc, data) => Emit(spc, generatorName, data, generate, warnWhenEmpty: false));
    }

    private static void Emit(
        SourceProductionContext spc,
        string generatorName,
        ImmutableArray<ToolExtraction> extractions,
        Action<SourceProductionContext, ImmutableArray<ToolModel>> generate,
        bool warnWhenEmpty)
    {
        foreach (var skipped in extractions.Where(it => it.Model is null))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                ToolSkippedRule, Location.None, skipped.TypeName, skipped.SkipReason));
        }

        var models = extractions
            .Where(it => it.Model is not null)
            .Select(it => it.Model!)
            .ToImmutableArray();

        if (models.IsEmpty)
        {
            if (warnWhenEmpty)
            {
                spc.ReportDiagnostic(Diagnostic.Create(NoToolsFoundRule, Location.None, generatorName));
            }

            return;
        }

        generate(spc, models);
    }

    /// <summary>
    /// Whether the assembly being compiled asked for this generator. Reduced to a bool here rather
    /// than carried as an <see cref="AttributeData"/>, which would drag a symbol into the pipeline.
    /// </summary>
    private static IncrementalValueProvider<bool> IsGeneratorEnabled<TAttribute>(
        IncrementalGeneratorInitializationContext context)
    {
        var attributeType = typeof(TAttribute);

        return context.CompilationProvider.Select((compilation, _) =>
        {
            var symbol = compilation.GetTypeByMetadataName($"{attributeType.Namespace}.{attributeType.Name}");

            return compilation.Assembly
                .GetAttributes()
                .Any(it => SymbolEqualityComparer.Default.Equals(it.AttributeClass, symbol));
        });
    }
}
