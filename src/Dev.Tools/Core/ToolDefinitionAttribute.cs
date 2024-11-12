namespace Dev.Tools.Core;

public sealed class ToolDefinitionAttribute : Attribute
{
    public string Name { get; set; } = default!;

    public string[] Aliases { get; set; } = [];

    public Categories[] Categories { get; set; } = [];

    public Keywords[] Keywords { get; set; } = [];

    public ErrorCodes[] ErrorCodes { get; set; } = [];
}