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
            TypeName = tool.TypeName + "Endpoint",
            GeneratorType = typeof(ApiEndpointGenerator),
            Placeholders = new()
            {
                ["ToolName"] = tool.Name,
                ["ToolType"] = tool.TypeName,
                ["ToolArgsType"] = tool.ArgsDetails.Type,
                ["ToolResultType"] = tool.ResultDetails.Type
            },
            Usings = [
                "Dev.Tools.Tools",
                "Dev.Tools.Api.Core",
                "Microsoft.AspNetCore.Mvc",
                "System.Net",
                "System.Net.Mime"
            ],
            Content = 
              """
              public class {TypeName}(Dev.Tools.Providers.IToolsProvider toolProvider) : EndpointBase
              {
                  [HttpPost("{ToolName}")]
                  [ProducesResponseType<{ToolResultType}>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
                  [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
                  [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.Json)]
                  public async Task<IActionResult> HandleAsync([FromBody] {ToolArgsType} request, CancellationToken cancellationToken)
                  {
                      try 
                      {  
                          var tool = toolProvider.GetTool<{ToolType}>();  
                          {ToolResultType} result = await tool.RunAsync(request, cancellationToken);
                          if (result.HasErrors)
                          {
                              return Problem(type: result.ErrorCodes[0].ToString(), statusCode: (int)HttpStatusCode.BadRequest);
                          }
                  
                          return Ok(result);
                      }
                      catch (Exception e)
                      {
                          return Problem(title: "Failed to run tool", detail: e.Message, statusCode: (int)HttpStatusCode.InternalServerError);
                      }
                  }
              }
              """
        };
}