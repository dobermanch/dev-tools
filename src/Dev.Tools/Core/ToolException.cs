namespace Dev.Tools.Core;

public sealed class ToolException(string errorCode) : Exception
{
    public string ErrorCode { get; } = errorCode;
}