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
        var title = ToolText.EscapeLiteral(tool.DisplayTitle);
        var description = ToolText.EscapeLiteral(tool.Description);
        var uploadable = tool.Args.Properties.FirstOrDefault(it => it.IsPipeInput && (it.IsContent || it.NonNullableType == "string"));

        return new CodeBlock
        {
            Namespace = "Dev.Tools.Api.Endpoints",
            TypeName = ToolNaming.EndpointType(tool.TypeName),
            GeneratorType = typeof(ApiEndpointGenerator),
            Placeholders = new()
            {
                ["ToolName"] = tool.Name,
                ["ToolType"] = tool.TypeName,
                ["ToolTitle"] = title,
                ["ToolDescription"] = description,
                ["ToolArgsType"] = requestType,
                ["ToolArgs"] = tool.Args.Type,
                ["ToolResultType"] = responseType,
                ["RequestType"] = ToolEmitter.DataType(tool, tool.Args, "Request"),
                ["RequestTypeMapping"] = ToolEmitter.Mapping(
                    tool.Args, tool.Args.Type, "args", it => ToolEmitter.ArgumentValue(it, $"request.{it.Name}")),
                ["FormRequestType"] = uploadable is null
                    ? string.Empty
                    : ToolEmitter.DataType(tool, tool.Args, "FormRequest",
                        typeOf: it => it.IsPipeInput ? "IFormFile" : it.BindingType),
                ["FormAction"] = uploadable is null
                    ? string.Empty
                    : GenerateFormAction(tool, title, description, responseType),
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
                "System.Net.Mime"
            ],
            Content =
              """
              public class {TypeName}(Dev.Tools.Providers.IToolsProvider toolProvider) : EndpointBase
              {
                  {RequestType}
                  {FormRequestType}
                  {ResultType}

                  [HttpPost("{ToolName}")]
                  [EndpointSummary("{ToolTitle}")]
                  [EndpointDescription("{ToolDescription}")]
                  [ProducesResponseType<{ToolResultType}>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
                  [ProducesResponseType<ToolProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
                  [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.ProblemJson)]
                  public async Task<IActionResult> HandleAsync([FromBody] {ToolArgsType} request, CancellationToken cancellationToken)
                  {
                      {RequestTypeMapping}

                      return await RunToolAsync(args, cancellationToken);
                  }
                  {FormAction}
                  private async Task<IActionResult> RunToolAsync({ToolArgs} args, CancellationToken cancellationToken)
                  {
                      // No catch here: an unexpected exception goes to the host's exception handler, which
                      // logs it and answers with a detail-free 500. Catching it here used to return
                      // exception.Message to the client.
                      var tool = toolProvider.GetTool<{ToolType}>();

                      var result = await tool.RunAsync(args, cancellationToken);
                      if (result.HasErrors)
                      {
                          return ToolFailed(result.Errors);
                      }

                      {ResultTypeMapping}

                      return Ok(response);
                  }
              }
              """
        };
    }

    /// <summary>
    /// The same tool taking its piped argument as an uploaded file, at <c>tools/{name}/file</c>. A tool
    /// that takes a string gets the file's text; a tool that takes <c>ToolContent</c> gets the file
    /// itself. The other arguments are form fields.
    ///
    /// Not the JSON route with a second content type: routing tells the two apart, but the OpenAPI
    /// document keeps one operation per path and method, and the JSON body disappeared from it.
    /// </summary>
    private static string GenerateFormAction(ToolModel tool, string title, string description, string responseType)
    {
        var formRequestType = ToolNaming.BaseName(tool.TypeName) + "FormRequest";
        var mapping = ToolEmitter.Mapping(tool.Args, tool.Args.Type, "args", it =>
            !it.IsPipeInput ? ToolEmitter.ArgumentValue(it, $"request.{it.Name}")
            : it.IsContent ? $"ContentOf(request.{it.Name})"
            : $"await ReadTextAsync(request.{it.Name}, cancellationToken)");

        return $$"""

                     [HttpPost("{{tool.Name}}/file")]
                     [Consumes(MediaTypeNames.Multipart.FormData)]
                     [RequestSizeLimit(MaxUploadBytes)]
                     [EndpointSummary("{{title}}")]
                     [EndpointDescription("{{description}}")]
                     [ProducesResponseType<{{responseType}}>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
                     [ProducesResponseType<ToolProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
                     [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.ProblemJson)]
                     public async Task<IActionResult> HandleFormAsync([FromForm] {{formRequestType}} request, CancellationToken cancellationToken)
                     {
                         {{mapping}}

                         return await RunToolAsync(args, cancellationToken);
                     }

                 """;
    }
}
