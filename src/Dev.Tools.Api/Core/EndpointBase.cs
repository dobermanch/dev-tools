using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Dev.Tools.Api.Core;

[ApiController]
[Route("tools")]
public abstract class EndpointBase : ControllerBase
{
    /// <summary>
    /// The response for a tool that reported errors. 400 when the input was at fault; 500 when the tool
    /// failed unexpectedly (<see cref="ErrorCode.Unknown"/>), which is the server's problem, not the
    /// caller's. Every error is listed, with its target spelled the way the request body spells it.
    /// </summary>
    protected ObjectResult ToolFailed(IReadOnlyList<ToolError> errors)
    {
        var unexpected = errors.Any(it => it.Code == ErrorCode.Unknown);
        var status = unexpected ? StatusCodes.Status500InternalServerError : StatusCodes.Status400BadRequest;
        var naming = HttpContext.RequestServices
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value
            .JsonSerializerOptions
            .PropertyNamingPolicy;

        var problem = new ToolProblemDetails
        {
            Type = unexpected
                ? "https://tools.ietf.org/html/rfc9110#section-15.6.1"
                : "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = unexpected ? "The tool failed unexpectedly" : "The tool rejected its input",
            Status = status,
            Errors = errors
                .Select(it => new ToolProblem(
                    it.Code,
                    it.Target is null ? null : naming?.ConvertName(it.Target) ?? it.Target,
                    it.Message))
                .ToArray()
        };
        problem.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }
}
