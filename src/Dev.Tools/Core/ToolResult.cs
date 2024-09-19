namespace Dev.Tools.Core;

public record ToolResult
{
#if !ANALIZER
    public bool HasErrors => ErrorCodes.Any();

    public IList<ToolError> ErrorCodes { get; } = [];
#endif
}
