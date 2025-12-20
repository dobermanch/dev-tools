using System.Collections.Immutable;
using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.CodeAnalysis.Generators;

/// <summary>
/// Generates a LocalizationKeysProvider class that contains all localization keys
/// without referencing tool types, making it safe to load via reflection.
/// </summary>
[Generator]
public class LocalizationKeysGenerator : ToolGeneratorBase, IIncrementalGenerator
{
    public class TrackingNames
    {
        public const string GetAssemblyInterface = nameof(GetAssemblyInterface);
        public const string GetTools = nameof(GetTools);
        public const string Combine = nameof(Combine);
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<bool> assemblyProvider =
            context
                .CompilationProvider
                .Select((compilation, _) => compilation.Assembly.Name == CodeDefinitions.ToolsAssemblyName)
                .WithTrackingName(TrackingNames.GetAssemblyInterface);

        IncrementalValuesProvider<ToolDetails> toolsProvider =
            context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    CodeDefinitions.ToolDefinitionAttribute.TypeFullName,
                    predicate: (s, _) => IsToolsClass(s),
                    transform: (ctx, _) => GetToolDetails((INamedTypeSymbol)ctx.TargetSymbol))
                .Where(it => it != null)
                .Select((it, _) => it!)
                .WithTrackingName(TrackingNames.GetTools);

        IncrementalValueProvider<ImmutableArray<ToolDetails>> toolsDeclarations =
            toolsProvider
                .Combine(assemblyProvider)
                .Select((it, _) => it.Left)
                .Collect()
                .WithTrackingName(TrackingNames.Combine);

        context.RegisterSourceOutput(toolsDeclarations, GenerateLocalizationKeys);
    }

    private static bool IsToolsClass(SyntaxNode node) =>
        node is ClassDeclarationSyntax definition
        && definition.HasAttribute(CodeDefinitions.ToolDefinitionAttribute.SyntaxTypeName);

    protected override void GenerateTools(SourceProductionContext spc, ImmutableArray<ToolDetails> tools)
    {
        GenerateLocalizationKeys(spc, tools);
    }

    private void GenerateLocalizationKeys(SourceProductionContext spc, ImmutableArray<ToolDetails> tools)
    {
        if (tools.Length <= 0)
        {
            return;
        }

        var localizationKeysCode = GenerateLocalizationKeysCode(tools);

        var codeBlock = new CodeBlock
        {
            Namespace = "Dev.Tools",
            TypeName = "LocalizationKeysProvider",
            GeneratorType = typeof(LocalizationKeysGenerator),
            Placeholders = new()
            {
                ["ToolKeys"] = localizationKeysCode
            },
            Usings =
            [
                "System.Collections.Generic"
            ],
            Content =
                """
                /// <summary>
                /// Provides localization keys for all tools without referencing tool types.
                /// This class can be safely loaded via reflection without triggering tool type loading.
                /// </summary>
                internal partial class {TypeName}
                {
                    public static IReadOnlyCollection<ToolLocalizationKeys> AllToolKeys { get; } =
                    [
                {ToolKeys}
                    ];
                }

                /// <summary>
                /// Represents localization keys for a single tool.
                /// </summary>
                public sealed record ToolLocalizationKeys(
                    string ToolName,
                    string[] Categories,
                    string[] Keywords,
                    string[] ErrorCodes,
                    PropertyLocalizationKeys[] ArgsProperties,
                    PropertyLocalizationKeys[] ResultProperties,
                    EnumLocalizationKeys[] Enums
                );

                /// <summary>
                /// Represents localization keys for a property.
                /// </summary>
                public sealed record PropertyLocalizationKeys(
                    string Name
                );

                /// <summary>
                /// Represents localization keys for an enum.
                /// </summary>
                public sealed record EnumLocalizationKeys(
                    string Name,
                    string[] Values
                );
                """
        };

        spc.AddSource(codeBlock.OutputFileName, codeBlock);
    }

    private static string GenerateLocalizationKeysCode(ImmutableArray<ToolDetails> tools)
    {
        var toolsCode = new List<string>();

        foreach (var tool in tools)
        {
            var className = tool.TypeName.Replace("Tool", "");

            // Generate args properties
            var argsProps = string.Join(", ",
                tool.ArgsDetails.Properties.Select(p => $"new PropertyLocalizationKeys(\"{p.Name}\")"));

            // Generate result properties
            var resultProps = string.Join(", ",
                tool.ResultDetails.Properties.Select(p => $"new PropertyLocalizationKeys(\"{p.Name}\")"));

            // Generate enums
            var enums = string.Join(", ",
                tool.ExtraTypes.OfType<EnumDetails>().Select(e =>
                    $"new EnumLocalizationKeys(\"{e.Type.Split('.').Last()}\", [{string.Join(", ", e.Values.Select(v => $"\"{v}\""))}])"));

            var categoriesStr = string.Join(", ", tool.Categories.Select(c => $"\"{c}\""));
            var keywordsStr = string.Join(", ", tool.Keywords.Select(k => $"\"{k}\""));
            var errorCodesStr = string.Join(", ", tool.ErrorCodes.Select(ec => $"\"{ec}\""));

            toolsCode.Add($"""
                        new ToolLocalizationKeys(
                            ToolName: "{className}",
                            Categories: [{categoriesStr}],
                            Keywords: [{keywordsStr}],
                            ErrorCodes: [{errorCodesStr}],
                            ArgsProperties: [{argsProps}],
                            ResultProperties: [{resultProps}],
                            Enums: [{enums}]
                        )
            """);
        }

        return string.Join(",\n", toolsCode).Replace("\t", "    ");
    }
}
