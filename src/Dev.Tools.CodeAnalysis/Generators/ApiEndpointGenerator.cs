using System.Collections.Immutable;
using Dev.Tools.Api.Core;
using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis;

namespace Dev.Tools.CodeAnalysis.Generators;

[Generator]
public class ApiEndpointGenerator : ToolGeneratorBase, IIncrementalGenerator
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

        var attributes = GetAssemblyAttribute<GenerateToolsApiEndpointAttribute>(context.CompilationProvider);
        
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
            var code = GenerateApiEndpoint(tool);
            spc.AddSource(code.OutputFileName, code);
        }
    }

    private CodeBlock GenerateApiEndpoint(ToolDetails tool) =>
        new()
        {
            Namespace = "Dev.Tools.Api.Endpoints",
            TypeName = tool.TypeName.Replace("Tool", "") + "Endpoint",
            GeneratorType = typeof(ApiEndpointGenerator),
            Placeholders = new()
            {
                ["ToolName"] = tool.Name,
                ["ToolType"] = tool.TypeName,
                ["ToolLocalizationName"] = tool.TypeName.Replace("Tool", ""),
                ["ToolArgsType"] = tool.TypeName.Replace("Tool", "Request"),
                ["ToolResultType"] = tool.TypeName.Replace("Tool", "Response"),
                ["RequestType"] = GenerateDataType(tool, tool.ArgsDetails, "Request"),
                ["RequestTypeMapping"] = GenerateDataTypeMapping(tool, tool.ArgsDetails, "args", "request"),
                ["ResultType"] = GenerateDataType(tool, tool.ResultDetails, "Response"),
                ["ResultTypeMapping"] = GenerateDataTypeMapping(tool, tool.ResultDetails, "response", "result")
            },
            Usings = [
                "Dev.Tools.Tools",
                "Dev.Tools.Api.Core",
                "Dev.Tools.Localization",
                "Microsoft.AspNetCore.Mvc",
                "System.Net",
                "System.Net.Mime"
            ],
            Content = 
              """
              public class {TypeName}(Dev.Tools.Providers.IToolsProvider toolProvider) : EndpointBase
              {
                  {RequestType}
                  
                  {ResultType}
                    
                  [HttpPost("{ToolName}")]
                  [LocalizedEndpointSummary("Tools.{ToolLocalizationName}.Name")]
                  [LocalizedEndpointDescription("Tools.{ToolLocalizationName}.Description")]
                  [ProducesResponseType<{ToolResultType}>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
                  [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
                  [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.Json)]
                  public async Task<IActionResult> HandleAsync([FromBody] {ToolArgsType} request, CancellationToken cancellationToken)
                  {
                      try 
                      {  
                          var tool = toolProvider.GetTool<{ToolType}>();
                          
                          {RequestTypeMapping}
                          
                          var result = await tool.RunAsync(args, cancellationToken);
                          if (result.HasErrors)
                          {
                              return Problem(type: result.ErrorCodes[0].ToString(), statusCode: (int)HttpStatusCode.BadRequest);
                          }
                          
                          {ResultTypeMapping}
                          
                          return Ok(response);
                      }
                      catch (Exception e)
                      {
                          return Problem(title: "Failed to run tool", detail: e.Message, statusCode: (int)HttpStatusCode.InternalServerError);
                      }
                  }
              }
              """
        };
    
    private static string GenerateRequestClass(ToolDetails tool)
    {
        var properties = new System.Text.StringBuilder();
        var toolName = tool.TypeName.Replace("Tool", "");
        
        for(var i = 0; i < tool.ArgsDetails.Properties.Length; i++)
        {
            var prop = tool.ArgsDetails.Properties[i];
            var description = $"Tools.{toolName}.{tool.ArgsDetails.TypeName}.{prop.Name}.Description";

            properties.AppendLine($$"""
                                            [LocalizedDescription("{{description}}")]
                                            public {{prop.Type}} {{prop.Name}} { get; set; }

                                    """);
        }

        return $$"""
                 public sealed class {{toolName}}Request
                     {
                 {{properties}}    }
                 """;
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
    
    private static string GenerateDataTypeMapping(ToolDetails tool, TypeDetails typeDetails, string targetVariableName, string sourceVariableName)
    {
        // Check if Args type has a constructor matching all properties (positional record)
        var hasMatchingConstructor = HasConstructorMatchingProperties(typeDetails.Symbol);

        if (hasMatchingConstructor)
        {
            // Use constructor syntax for positional records
            var arguments = string.Join(",\n                    ",
                typeDetails.Properties.Select(p => $"{p.Name}: {sourceVariableName}.{p.Name}"));

            return $$"""
                     var {{targetVariableName}} = new {{typeDetails.Type}}(
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
                     var {{targetVariableName}} = new {{typeDetails.Type}}
                                {
                                    {{properties}}
                                };
                     """;
        }
    }
}