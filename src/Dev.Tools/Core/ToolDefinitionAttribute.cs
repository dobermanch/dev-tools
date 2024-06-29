namespace Dev.Tools.Core;

public class ToolDefinitionAttribute : Attribute
{
    public required string Name { get; init; }

    public string[] Aliases { get; init; } = [];

    public ToolCategory[] Categories { get; init; } = [];

    public ToolKeyword[] Keywords { get; init; } = [];

    public ToolError[] ErrorCodes { get; init; } = [];
}