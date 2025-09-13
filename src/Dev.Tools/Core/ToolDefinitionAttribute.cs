namespace Dev.Tools.Core;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class ToolDefinitionAttribute : Attribute
{
    public required string Name { get; set; }
    public string[] Aliases { get; set; } = [];
    public string[] Categories { get; set; } = [];
    public Keyword[] Keywords { get; set; } = [];
}

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ToolApiGenerationAttribute : Attribute;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ToolCliCommandGenerationAttribute : Attribute;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ToolMcpCommandGenerationAttribute : Attribute;