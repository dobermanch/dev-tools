namespace Dev.Tools.Core;

public interface ITool;

public interface ITool<in TArg, TResult> : ITool
    where TResult : ToolResult
{
    Task<TResult> RunAsync(TArg args, CancellationToken cancellationToken);
}