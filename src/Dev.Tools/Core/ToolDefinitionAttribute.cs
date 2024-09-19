namespace Dev.Tools.Core;

public class ToolDefinitionAttribute : Attribute
{
#if !ANALIZER
    public required string Name { get; init; }

    public string[] Aliases { get; init; } = [];

    public string[] Categories { get; init; } = [];

    public string[] Keywords { get; init; } = [];

    public string[] ErrorCodes { get; init; } = [];
#endif
}