namespace Dev.Tools.Core;

public record ToolResult
{
    public bool HasErrors => ErrorCodes.Any();

    public IList<ToolError> ErrorCodes { get; } = [];
}
