using System.Collections.Immutable;
using Dev.Tools.CodeAnalysis.Core;
using Dev.Tools.Console.Core;
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

    private CodeBlock GenerateTool(ToolDetails tool) =>
        new()
        {
            Namespace = "Dev.Tools.Mcp.Tools",
            TypeName = tool.TypeName,
            GeneratorType = typeof(McpToolGenerator),
            Placeholders = new()
            {
                ["ToolName"] = tool.Name,
                ["ToolType"] = tool.TypeName,
                ["ToolArgsType"] = tool.ArgsDetails.TypeName,
                ["ToolResultType"] = tool.ResultDetails.TypeName
            },
            Usings = [
                "System.ComponentModel",
                "Dev.Tools.Providers",
                "Dev.Tools.Tools",
                "ModelContextProtocol.Server"
            ],
            Content = 
              """
              [McpServerToolType]
              public static class {TypeName}
              {
                  [McpServerTool, Description("")]
                  public static async Task<{ToolType}.Result> Decode1(
                      IToolsProvider tools,
                      [Description("")]
                      {ToolType}.{ToolArgsType} args,
                      CancellationToken cancellationToken)
                  {
                      var result = await tools
                          .GetTool<{ToolType}>()
                          .RunAsync(
                              args,
                              //new {ToolType}.{ToolArgsType} { Text = input },
                              cancellationToken
                          );
                      return result;
                  }
              }
              """
        };
}