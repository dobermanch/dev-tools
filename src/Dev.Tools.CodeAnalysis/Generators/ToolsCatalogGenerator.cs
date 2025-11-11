using System.Collections.Immutable;
using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.CodeAnalysis.Generators;

[Generator]
public class ToolsCatalogGenerator : ToolGeneratorBase, IIncrementalGenerator
{
    public class TrackingNames
    {
        public const string GetAssemblyInterface = nameof(GetAssemblyInterface);
        public const string GetTools = nameof(GetTools);
        public const string Combine = nameof(Combine);
    }
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // context.RegisterPostInitializationOutput(ctx =>
        // {
        //     ctx.AddSource(CodeDefinitions.ToolDefinitionAttribute.OutputFileName,
        //         CodeDefinitions.ToolDefinitionAttribute);
        //     ctx.AddSource(CodeDefinitions.ToolDefinition.OutputFileName, CodeDefinitions.ToolDefinition);
        // });
       // Debugger.Launch();
        
        //var guardAttribute = typeof(GenerateToolsAttribute);
        IncrementalValueProvider<bool> assemblyProvider =
            context
                .CompilationProvider
                .Select((compilation, _) =>
                {
                    //var attrSymbol = compilation.GetTypeByMetadataName($"{guardAttribute.Namespace}.{guardAttribute.Name}");
                    return compilation.Assembly.Name == CodeDefinitions.ToolsAssemblyName;
                        //.GetAttributes()
                        //.FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attrSymbol));
                })
                .WithTrackingName(TrackingNames.GetAssemblyInterface)
            ;

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
                //.Where(it => it.Right is not null)
                //.Where(it => it.Right)
                .Select((it, _) => it.Left)
                .Collect()
                .WithTrackingName(TrackingNames.Combine);

        context.RegisterSourceOutput(toolsDeclarations, GenerateTools);
    }

    private static bool IsToolsClass(SyntaxNode node) =>
        node is ClassDeclarationSyntax definition
        && definition.HasAttribute(CodeDefinitions.ToolDefinitionAttribute.SyntaxTypeName);

    protected override void GenerateTools(SourceProductionContext spc, ImmutableArray<ToolDetails> tools)
    {
        if (tools.Length <= 0)
        {
            return;
        }
        
        string Aggregate(IList<string> data)
        {
            return "[" + string.Join(", ", data) + "]";
        } 

        string toolsString = tools.Select(it =>
            $"""
                     new ToolDefinition(
                         Name: "{it.Name}",
                         Aliases: {Aggregate(it.Aliases)},
                         Categories: {Aggregate(it.Categories)},
                         Keywords: {Aggregate(it.Keywords)},
                         ErrorCodes: {Aggregate(it.ErrorCodes)},
                         ToolType: typeof({it.Namespace}.{it.TypeName}),
                         ArgsType: new ToolDefinition.TypeDetails(
                             Name: "{it.ArgsDetails.Type.Split('.').Last()}",
                             DataType: typeof({it.ArgsDetails.Type}),
                             Properties: [
                                 {string.Join(",\n\t\t\t\t\t", it.ArgsDetails.Properties.Select(x => $"new ToolDefinition.TypeProperty(\"{x.Name}\", typeof({x.Type.Replace("?", "")}), {x.IsRequired.ToString().ToLower()}, {x.IsNullable.ToString().ToLower()})"))}
                             ]
                         ),
                         ReturnType: new ToolDefinition.TypeDetails(
                             Name: "{it.ResultDetails.Type.Split('.').Last()}",
                             DataType: typeof({it.ResultDetails.Type}),
                             Properties: [
                                 {string.Join(",\n\t\t\t\t\t", it.ResultDetails.Properties.Select(x => $"new ToolDefinition.TypeProperty(\"{x.Name}\", typeof({x.Type.Replace("?", "")}), {x.IsRequired.ToString().ToLower()}, {x.IsNullable.ToString().ToLower()})"))}
                             ]
                         ),
                         ExtraTypes: [
                             {string.Join(",\n\t\t\t\t", it.ExtraTypes.OfType<EnumDetails>().Select(x => $"new ToolDefinition.EnumDetails(\"{x.Type.Split('.').Last()}\", typeof({x.Type}), [{string.Join(", ", x.Values.Select(v => $"\"{v}\""))}])"))}
                         ]
                     ),
             """)
            .Aggregate("", (current, it) => current + it + "\n")
            .Replace("\t", "    ");

        var codeBlock = new CodeBlock
        {
            Namespace = "Dev.Tools",
            TypeName = "ToolsCatalog",
            GeneratorType = typeof(ToolsCatalogGenerator),
            Placeholders = new()
            {
                ["Tools"] = toolsString
            },
            Usings =
            [
                "System.Collections.Generic"
            ],
            Content =
                """
                internal partial class {TypeName}
                {
                    public static IReadOnlyCollection<ToolDefinition> ToolDefinitions { get; } = 
                    [
                {Tools}
                    ];
                }
                """
        };
        
        spc.AddSource(codeBlock.OutputFileName, codeBlock);
    }
}