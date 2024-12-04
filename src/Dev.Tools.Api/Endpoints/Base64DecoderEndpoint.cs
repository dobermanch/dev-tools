using Dev.Tools.Api.Core;
using Dev.Tools.Tools;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mime;

namespace Dev.Tools.Api.Endpoints;

public class Base64DecoderEndpoint(Base64DecoderTool tool) : EndpointBase
{
    [HttpPost("base64-decoder1")]
    [EndpointName("base64-decoder1")]
    [EndpointSummary("Summary: Base64 decoder base64 encoded text")]
    [EndpointDescription("Description: Base64 decoder base64 encoded text")]
    [Tags("Converters")]
    [ProducesResponseType<ResponseDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.Json)]
    public async Task<IResult> HandleAsync([FromBody] RequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var args = new Base64DecoderTool.Args(
                request.Text
            );

            Base64DecoderTool.Result result = await tool.RunAsync(args, cancellationToken);

            if (result.HasErrors)
            {
                return Results.Problem(type: result.ErrorCodes[0], statusCode: (int)HttpStatusCode.BadRequest);
            }

            return Results.Ok(new ResponseDto
            {
                Text = result.Data
            });
            
        }
        catch (Exception e)
        {
            return Results.Problem(title:"The base64-decoder1 failed.", type: "Unhandled", detail: e.Message, statusCode: (int)HttpStatusCode.BadRequest);
        }
    }

    public record RequestDto
    {
        public string Text { get; init; } = default!;
    }

    public record ResponseDto
    {
        public string Text { get; init; } = default!;
    }
}
