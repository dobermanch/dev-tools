using Microsoft.AspNetCore.Mvc;

namespace Dev.Tools.Api.Core;

/// <summary>
/// RFC 9457 problem details for a tool that could not produce its result, with every error the tool
/// reported. Clients branch on <see cref="Errors"/>. The problem <c>type</c> stays the plain HTTP status
/// type, because one response may carry several different reasons.
/// </summary>
public sealed class ToolProblemDetails : ProblemDetails
{
    public IReadOnlyList<ToolProblem> Errors { get; init; } = [];
}

/// <param name="Code">The tool's culture-neutral reason, e.g. <c>TextEmpty</c>.</param>
/// <param name="Target">
/// The request property the error is about, spelled as it is in the request body; null when the error is
/// about the call as a whole.
/// </param>
/// <param name="Message">The English description of <paramref name="Code"/>.</param>
public sealed record ToolProblem(ErrorCode Code, string? Target, string Message);
