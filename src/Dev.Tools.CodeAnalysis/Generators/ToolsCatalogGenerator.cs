using System.Collections.Immutable;
using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis;

namespace Dev.Tools.CodeAnalysis.Generators;

[Generator]
public class ToolsCatalogGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
        => ToolPipeline.RegisterSourceTools(context, "tools catalog", Generate);

    private static void Generate(SourceProductionContext spc, ImmutableArray<ToolModel> tools)
    {
        string toolsString = tools
            .Select(Definition)
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

    private static string Definition(ToolModel tool) =>
        $"""
                 new ToolDefinition(
                     Name: "{tool.Name}",
                     Title: "{ToolText.EscapeLiteral(tool.DisplayTitle)}",
                     Description: "{ToolText.EscapeLiteral(tool.Description)}",
                     Usage: "{ToolText.EscapeLiteral(tool.Usage)}",
                     Aliases: {Aggregate(tool.Aliases)},
                     Categories: {Aggregate(tool.Categories)},
                     Keywords: {Aggregate(tool.Keywords)},
                     ErrorCodes: {Aggregate(tool.ErrorCodes)},
                     ToolType: typeof({tool.Namespace}.{tool.TypeName}),
                     ArgsType: new ToolDefinition.TypeDetails(
                         Name: "{tool.Args.SimpleName}",
                         DataType: typeof({tool.Args.Type}),
                         Properties: [
                             {Properties(tool.Args)}
                         ]
                     ),
                     ReturnType: new ToolDefinition.TypeDetails(
                         Name: "{tool.Result.SimpleName}",
                         DataType: typeof({tool.Result.Type}),
                         Properties: [
                             {Properties(tool.Result)}
                         ]
                     ),
                     ExtraTypes: [
                         {Enums(tool)}
                     ]
                 ),
         """;

    private static string Aggregate(EquatableArray<string> values)
        => "[" + string.Join(", ", values) + "]";

    private static string Properties(TypeModel type)
        => string.Join(",\n\t\t\t\t\t", type.Properties.Select(it =>
            $"new ToolDefinition.TypeProperty(\"{it.Name}\", typeof({it.NonNullableType}), "
            + $"{Bool(it.IsRequired)}, {Bool(it.IsNullable)}, {Bool(it.IsPipeInput)}, {Bool(it.IsPipeOutput)}, "
            + $"\"{ToolText.EscapeLiteral(it.Description)}\", \"{ToolText.EscapeLiteral(it.DisplayName)}\")"));

    private static string Enums(ToolModel tool)
        => string.Join(",\n\t\t\t\t", tool.Enums.Select(it =>
            $"new ToolDefinition.EnumDetails(\"{it.SimpleName}\", typeof({it.Type}), ["
            + string.Join(", ", it.Values.Select(value =>
                $"new ToolDefinition.EnumValue(\"{value.Name}\", \"{ToolText.EscapeLiteral(value.Description)}\")"))
            + "])"));

    private static string Bool(bool value) => value ? "true" : "false";
}
