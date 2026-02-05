using System.Collections.Immutable;
using Dev.Tools.CodeAnalysis.Core;
using Dev.Tools.Mcp.Core;
using Microsoft.CodeAnalysis;

namespace Dev.Tools.CodeAnalysis.Generators;

[Generator]
public class McpToolGenerator : ToolGeneratorBase, IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var referencedToolTypes =
            context.CompilationProvider
                .SelectMany((compilation, _) =>
                    compilation
                        .References
                        .Select(compilation.GetAssemblyOrModuleSymbol)
                        .OfType<IAssemblySymbol>()
                        .Where(it => it.Name == CodeDefinitions.ToolsAssemblyName)
                        .Select(it => it.GlobalNamespace)
                        .SelectMany(it => it.GetAllTypes(t => t.HasAttribute(CodeDefinitions.ToolDefinitionAttribute.TypeFullName)))
                        .Select(it => GetToolDetails((INamedTypeSymbol)it))
                        .Where(it => it is not null)
                        .Select(it => it!)
                );

        var attributes = GetAssemblyAttribute<GenerateToolsMcpToolAttribute>(context.CompilationProvider);

        var compilationAndAttributes = referencedToolTypes
            .Combine(attributes)
            .Where(it => it.Right != null)
            .Select((it, _) => it.Left)
            .Collect();

        context.RegisterSourceOutput(compilationAndAttributes, GenerateTools);
    }

    protected override void GenerateTools(SourceProductionContext spc, ImmutableArray<ToolDetails> tools)
    {
        foreach (var tool in tools)
        {
            var code = GenerateTool(tool);
            spc.AddSource(code.OutputFileName, code);
        }
    }

    private CodeBlock GenerateTool(ToolDetails tool)
    {
        var argsClass = GenerateArgsClass(tool);
        var executeMethod = GenerateExecuteAsyncMethod(tool);

        return new()
        {
            Namespace = "Dev.Tools.Mcp.Tools",
            TypeName = tool.TypeName.Replace("Tool", "McpTool"),
            GeneratorType = typeof(McpToolGenerator),
            Usings = [
                "System.ComponentModel",
                "Dev.Tools.Providers",
                "Dev.Tools.Tools",
                "ModelContextProtocol.Server"
            ],
            Content =
              $$"""
              /// <summary>
              /// MCP tool wrapper for {{tool.TypeName}}.
              /// </summary>
              [McpServerToolType]
              public static class {{tool.TypeName}}McpTool
              {
              {{argsClass}}

              {{executeMethod}}
              }
              """
        };
    }

    private static string GenerateArgsClass(ToolDetails tool)
    {
        var properties = new System.Text.StringBuilder();

        foreach (var prop in tool.ArgsDetails.Properties)
        {
            // TODO: Extract description from ToolResources.en.resx
            // For now, use placeholder description
            var description = $"{prop.Name} parameter";

            properties.AppendLine($$"""
                    [Description("{{description}}")]
                    public {{prop.Type}} {{prop.Name}} { get; set; }

            """);
        }

        return $$"""
            public sealed class Args
            {
        {{properties}}    }
        """;
    }

    private static string GenerateExecuteAsyncMethod(ToolDetails tool)
    {
        var propertyMapping = GenerateMapping(tool);

        // TODO: Extract description from ToolResources.en.resx
        var toolDescription = $"{tool.TypeName} tool";

        return $$"""
            [McpServerTool(Name = "{{tool.Name}}")]
            [Description("{{toolDescription}}")]
            public static async Task<{{tool.TypeName}}.Result> ExecuteAsync(
                IToolsProvider tools,
                Args args,
                CancellationToken cancellationToken = default)
            {
                var tool = tools.GetTool<{{tool.TypeName}}>();

                {{propertyMapping}}

                var result = await tool.RunAsync(toolArgs, cancellationToken);
                return result;
            }
        """;
    }
    
    private static string GenerateMapping(ToolDetails tool)
    {
        // Check if Args type has a constructor matching all properties (positional record)
        var hasMatchingConstructor = HasConstructorMatchingProperties(tool.ArgsDetails.Symbol);

        if (hasMatchingConstructor)
        {
            // Use constructor syntax for positional records
            var arguments = string.Join(",\n                    ",
                tool.ArgsDetails.Properties.Select(p => $"{p.Name}: args.{p.Name}"));

            return $$"""
                     var toolArgs = new {{tool.TypeName}}.Args(
                                    {{arguments}}
                                );
                     """;
        }
        else
        {
            // Use object initializer syntax for init properties
            var properties = string.Join(",\n                    ",
                tool.ArgsDetails.Properties.Select(p => $"{p.Name} = args.{p.Name}"));

            return $$"""
                     var toolArgs = new {{tool.TypeName}}.Args
                                {
                                    {{properties}}
                                };
                     """;
        }
    }
}