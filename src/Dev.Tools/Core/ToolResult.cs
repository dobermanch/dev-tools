namespace Dev.Tools.Core;

public record ToolResult
{
    public bool HasErrors => ErrorCodes.Any();

    public IList<ErrorCodes> ErrorCodes { get; } = [];
}
