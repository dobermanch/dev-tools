namespace Dev.Tools;

/// <summary>
/// Thrown by a tool to report why it cannot produce a result. <see cref="ToolBase{TArgs,TResult}"/> turns it
/// into a failed result rather than letting it escape.
/// </summary>
public sealed class ToolException(IReadOnlyList<ToolError> errors)
    : Exception(string.Join("; ", errors.Select(it => it.Message)))
{
    /// <param name="errorCode">Why the tool failed.</param>
    /// <param name="target">The <c>Args</c> property at fault - pass <c>nameof(Args.X)</c> - or null.</param>
    public ToolException(ErrorCode errorCode, string? target = null)
        : this([new ToolError(errorCode, target)])
    {
    }

    public IReadOnlyList<ToolError> Errors { get; } = errors.Count > 0
        ? errors
        : throw new ArgumentException("A tool exception has to carry at least one error.", nameof(errors));
}
