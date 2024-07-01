using Dev.Tools.Api.Core;
using Dev.Tools.Tools;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Dev.Tools.Api.Endpoints;

public class Base64DecoderEndpoint(Base64DecoderTool tool) : EndpointBase
{
    [HttpPost("base64-decoder")]
    [SwaggerOperation(
        Summary = "Decode Base64",
        Description = "Decode Base64 string",
        OperationId = "base64-decoder",
        Tags = ["Converters"])
    ]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> HandleAsync([FromBody] RequestDto request, CancellationToken cancellationToken)
    {
        var args = new Base64DecoderTool.Args(
            request.Text
        );

        Base64DecoderTool.Result result = await tool.RunAsync(args, cancellationToken);

        if (result.HasErrors)
        {
            return Problem(type: result.ErrorCodes[0], statusCode: (int)HttpStatusCode.BadRequest);
        }

        return Ok(new
        {
            result.Text
        });
    }

    public record RequestDto
    {
        [SwaggerSchema(
            Title = "Text to decode",
            Description = "THe base64 string to decode"
        )]
        public string Text { get; init; } = default!;
    }

    public record ResponseDto
    {
        [SwaggerSchema(
            Title = "The baser64 string",
            Description = "Encoded base64 string"
        )]
        public string Text { get; init; } = default!;
    }
}
