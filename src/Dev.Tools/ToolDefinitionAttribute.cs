namespace Dev.Tools;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class ToolDefinitionAttribute : Attribute
{
    public required string Name { get; set; }
    public string[] Aliases { get; set; } = [];
    public Category[] Categories { get; set; } = [];
    public Keyword[] Keywords { get; set; } = [];
}