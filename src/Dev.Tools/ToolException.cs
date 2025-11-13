namespace Dev.Tools;

public sealed class ToolException(ErrorCode errorCode) : Exception
{
    public ErrorCode ErrorCode { get; } = errorCode;
}