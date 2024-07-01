using Dev.Tools.Api.Core;
using Dev.Tools.Tools;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Dev.Tools.Api.Endpoints;

public class Base64EncoderEndpoint(Base64EncoderTool tool) : EndpointBase
{
    [SwaggerOperation(
        Summary = "Encode Base64",
        Description = "Encode string to Base64 string",
        OperationId = "base64-encoder",
        Tags = ["Converters"])
    ]
    [HttpPost("base64-encoder")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public async Task<ActionResult> HandleAsync([FromBody] RequestDto request, CancellationToken cancellationToken)
    {
        var args = new Base64EncoderTool.Args(
            request.Text,
            request.InsertLineBreaks,
            request.UrlSafe
        );

        Base64EncoderTool.Result result = await tool.RunAsync(args, cancellationToken);

        if (result.HasErrors)
        {
            return Problem(type: result.ErrorCodes[0]);
        }

        return Ok(new
        {
            result.Text
        });
    }

    public record RequestDto
    {
        [SwaggerSchema(
            Title = "Text to encode",
            Description = "Text to encode to base64 string"
        )]
        public string Text { get; init; } = default!;

        [SwaggerSchema(
            Title = "Insert line breaks",
            Description = "Insert line breaks when string londer than 72 characters"
        )]
        public bool InsertLineBreaks { get; init; }

        [SwaggerSchema(
            Title = "Encode URL safe",
            Description = "Encode URL safe"
        )]
        public bool UrlSafe { get; init; }
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