namespace Dev.Tools.Core;

public interface ITool;

public interface IToolAsync<in TArg, out TResult> : ITool
    where TArg : ToolArgs
    where TResult : ToolResult
{
    TResult Run(TArg args);
}

public interface ITool<in TArg, TResult> : ITool
    where TArg : ToolArgs
    where TResult : ToolResult
{
    Task<TResult> RunAsync(TArg args, CancellationToken cancellationToken);
}