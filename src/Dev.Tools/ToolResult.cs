namespace Dev.Tools;

public record ToolResult
{
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Why the tool could not produce its result; empty on success. A tool may report several at once -
    /// every argument it rejected, rather than only the first.
    /// </summary>
    public IReadOnlyList<ToolError> Errors { get; init; } = [];
}
