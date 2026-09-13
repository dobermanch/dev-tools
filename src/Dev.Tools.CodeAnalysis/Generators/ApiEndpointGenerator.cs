using System.Collections.Immutable;
using Dev.Tools.Api.Core;
using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis;

namespace Dev.Tools.CodeAnalysis.Generators;

[Generator]
public class ApiEndpointGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
        => ToolPipeline.RegisterFrontend<GenerateToolsApiEndpointAttribute>(context, "API endpoint", Generate);

    private static void Generate(SourceProductionContext spc, ImmutableArray<ToolModel> tools)
    {
        foreach (var tool in tools)
        {
            var code = GenerateApiEndpoint(tool);
            spc.AddSource(code.OutputFileName, code);
        }
    }

    private static CodeBlock GenerateApiEndpoint(ToolModel tool)
    {
        var requestType = ToolNaming.RequestType(tool.TypeName);
        var responseType = ToolNaming.ResponseType(tool.TypeName);

        return new CodeBlock
        {
            Namespace = "Dev.Tools.Api.Endpoints",
            TypeName = ToolNaming.EndpointType(tool.TypeName),
            GeneratorType = typeof(ApiEndpointGenerator),
            Placeholders = new()
            {
                ["ToolName"] = tool.Name,
                ["ToolType"] = tool.TypeName,
                ["ToolTitle"] = ToolText.EscapeLiteral(tool.DisplayTitle),
                ["ToolDescription"] = ToolText.EscapeLiteral(tool.Description),
                ["ToolArgsType"] = requestType,
                ["ToolResultType"] = responseType,
                ["RequestType"] = ToolEmitter.DataType(tool, tool.Args, "Request"),
                ["RequestTypeMapping"] = ToolEmitter.Mapping(
                    tool.Args, tool.Args.Type, "args", it => $"request.{it.Name}"),
                ["ResultType"] = ToolEmitter.DataType(tool, tool.Result, "Response"),
                ["ResultTypeMapping"] = ToolEmitter.Mapping(
                    tool.Result, responseType, "response", it => $"result.{it.Name}",
                    forceObjectInitializer: true)
            },
            Usings = [
                "System.ComponentModel",
                "Dev.Tools.Tools",
                "Dev.Tools.Api.Core",
                "Microsoft.AspNetCore.Http",
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
                  [EndpointSummary("{ToolTitle}")]
                  [EndpointDescription("{ToolDescription}")]
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
    }
}
