namespace Dev.Tools.Core;

public readonly record struct ToolCategory(params ToolKeyword[] Keywords)
{
    public static readonly ToolCategory Converter = new(ToolKeyword.Convert, ToolKeyword.String, ToolKeyword.Text);
    public static readonly ToolCategory Text = new(ToolKeyword.Text, ToolKeyword.String);
    public static readonly ToolCategory Misc = new(ToolKeyword.Misc);
}
