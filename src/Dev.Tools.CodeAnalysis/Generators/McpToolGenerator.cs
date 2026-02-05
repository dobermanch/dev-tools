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
        return new()
        {
            Namespace = "Dev.Tools.Mcp.Tools",
            TypeName = tool.TypeName.Replace("Tool", "McpTool"),
            GeneratorType = typeof(McpToolGenerator),
            Placeholders = new()
            {
                ["ToolName"] = tool.Name,
                ["ToolType"] = tool.TypeName,
                ["ToolLocalizationName"] = tool.TypeName.Replace("Tool", ""),
                ["ToolArgsType"] = tool.TypeName.Replace("Tool", "Request"),
                ["ToolResultType"] = tool.TypeName.Replace("Tool", "Response"),
                ["RequestType"] = GenerateDataType(tool, tool.ArgsDetails, "Request"),
                ["RequestTypeMapping"] = GenerateDataTypeMapping(tool.ArgsDetails, tool.ArgsDetails.Type, "args", "request", false),
                ["ResultType"] = GenerateDataType(tool, tool.ResultDetails, "Response"),
                ["ResultTypeMapping"] = GenerateDataTypeMapping(tool.ResultDetails, tool.TypeName.Replace("Tool", "Response"), "response", "result", true)
            },
            Usings = [
                "System.ComponentModel",
                "Dev.Tools.Providers",
                "Dev.Tools.Tools",
                "Dev.Tools.Localization",
                "ModelContextProtocol.Server"
            ],
            Content =
              $$"""
              [McpServerToolType]
              public static class {{tool.TypeName}}McpTool
              {
                  {RequestType}
                  
                  {ResultType}

                  [McpServerTool(Name = "{ToolName}")]
                  [LocalizedDescription("Tools.{ToolLocalizationName}.Description")]
                  public static async Task<{ToolResultType}> ExecuteAsync(
                      IToolsProvider tools,
                      {ToolArgsType} request,
                      CancellationToken cancellationToken = default)
                  {
                      var tool = tools.GetTool<{ToolType}>();
                  
                      {RequestTypeMapping}
                  
                      var result = await tool.RunAsync(args, cancellationToken);
                      
                      {ResultTypeMapping}
                      
                      return response;
                  }
              }
              """
        };
    }
    
   private static string GenerateDataType(ToolDetails tool, TypeDetails typeDetails, string toolNameSuffix)
    {
        var properties = new System.Text.StringBuilder();
        var toolName = tool.TypeName.Replace("Tool", "");
        
        foreach (var prop in typeDetails.Properties)
        {
            var description = $"Tools.{toolName}.{typeDetails.TypeName}.{prop.Name}.Description";
            
            properties.AppendLine($$"""
                                            [LocalizedDescription("{{description}}")]
                                            public {{prop.Type}} {{prop.Name}} { get; set; }

                                    """);
        }

        return $$"""
                 public sealed class {{toolName}}{{toolNameSuffix}}
                     {
                 {{properties}}    }
                 """;
    }
    
    private static string GenerateDataTypeMapping(TypeDetails typeDetails, string typeName, string targetVariableName, string sourceVariableName, bool useParameterInitializer)
    {
        // Check if Args type has a constructor matching all properties (positional record)
        if (!useParameterInitializer && HasConstructorMatchingProperties(typeDetails.Symbol))
        {
            // Use constructor syntax for positional records
            var arguments = string.Join(",\n                    ",
                typeDetails.Properties.Select(p => $"{p.Name}: {sourceVariableName}.{p.Name}"));

            return $$"""
                     var {{targetVariableName}} = new {{typeName}}(
                                    {{arguments}}
                                );
                     """;
        }
        else
        {
            // Use object initializer syntax for init properties
            var properties = string.Join(",\n                    ",
                typeDetails.Properties.Select(p => $"{p.Name} = {sourceVariableName}.{p.Name}"));

            return $$"""
                     var {{targetVariableName}} = new {{typeName}}
                                {
                                    {{properties}}
                                };
                     """;
        }
    }
}