namespace Dev.Tools.Core;

public readonly record struct ToolKeyword
{
    public static readonly ToolKeyword Convert = new();
    public static readonly ToolKeyword String = new();
    public static readonly ToolKeyword Text = new();
    public static readonly ToolKeyword Decode = new();
    public static readonly ToolKeyword Encode = new();
    public static readonly ToolKeyword Base64 = new();
    public static readonly ToolKeyword Misc = new();
    public static readonly ToolKeyword Url = new();
    public static readonly ToolKeyword Uuid = new();
    public static readonly ToolKeyword Generate = new();
    public static readonly ToolKeyword Guid = new();
}
