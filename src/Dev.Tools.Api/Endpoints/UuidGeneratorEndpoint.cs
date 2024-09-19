using Dev.Tools.Api.Core;
using Dev.Tools.Tools;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Dev.Tools.Api.Endpoints;

public class UuidGeneratorEndpoint(UuidGeneratorTool tool) : EndpointBase
{
    [SwaggerOperation(
        Summary = "Generate UUID",
        Description = "Generates diferent version of UUID",
        OperationId = "uuid-gen",
        Tags = ["Generators"])
    ]
    [HttpPost("uuid-gen")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public async Task<ActionResult> HandleAsync([FromBody] RequestDto request, CancellationToken cancellationToken)
    {
        var args = new UuidGeneratorTool.Args(
            request.Type,
            request.Count,
            request.Namespace,
            request.Name,
            request.Time
        );

        UuidGeneratorTool.Result result = await tool.RunAsync(args, cancellationToken);

        if (result.HasErrors)
        {
            return Problem(type: result.ErrorCodes[0]);
        }


        return Ok(new ResponseDto
        {
            Uuids = result.Uuids
        });
    }

    public record RequestDto
    {
        [SwaggerSchema(
            Title = "UUID Version",
            Description = "UUID version"
        )]
        public UuidGeneratorTool.UuidType Type { get; init; }

        [SwaggerSchema(
            Title = "UUID quantity",
            Description = "Number of UUID to generate"
        )]
        public int Count { get; init; }

        [SwaggerSchema(
            Title = "UUID Namesapce",
            Description = "Namespace for V3 | V5 UUID version"
        )]
        public Guid? Namespace { get; init; }

        [SwaggerSchema(
            Title = "Name",
            Description = "Name for V3|V5 UUID version"
        )]
        public string? Name { get; init; }

        [SwaggerSchema(
            Title = "Timestamp",
            Description = "Timestamp (ISO 8601) for V7 UUID version"
        )]
        public DateTimeOffset? Time { get; init; }
    }

    public record ResponseDto
    {
        [SwaggerSchema(
            Title = "Generated UUIDs",
            Description = "Generated UUIDs"
        )]
        public IReadOnlyCollection<Guid> Uuids { get; init; } = [];
    }
}