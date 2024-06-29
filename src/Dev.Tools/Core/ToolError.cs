namespace Dev.Tools.Core;

public readonly record struct ToolError(string Code, string? Message = null)
{
    public static readonly ToolError Unknown = new("UNKNOWN");

    public static implicit operator ToolError(string code) => new(code);

    public static implicit operator string(ToolError code) => code.Code;
}