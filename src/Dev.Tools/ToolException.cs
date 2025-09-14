namespace Dev.Tools;

public sealed class ToolException(string errorCode) : Exception
{
    public string ErrorCode { get; } = errorCode;
}