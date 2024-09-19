namespace Dev.Tools.Core;

public sealed class ToolDefinitionAttribute : Attribute
{
    public string Name { get; set; } = default!;

    public string[] Aliases { get; set; } = [];

    public string[] Categories { get; set; } = [];

    public string[] Keywords { get; set; } = [];

    public string[] ErrorCodes { get; set; } = [];
}