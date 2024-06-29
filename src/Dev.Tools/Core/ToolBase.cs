namespace Dev.Tools.Core;

public abstract class ToolBase<TArgs, TResult> : ITool<TArgs, TResult>
    where TArgs : ToolArgs
    where TResult: ToolResult, new()
{
    public async Task<TResult> RunAsync(TArgs args, CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteAsync(args, cancellationToken);
        }
        catch (Exception ex)
        {
            return Failed(new(ToolError.Unknown.Code, ex.Message));
        }
    }

    protected virtual Task<TResult> ExecuteAsync(TArgs args, CancellationToken cancellationToken)
    {
        return Task.FromResult(Execute(args));
    }

    protected virtual TResult Execute(TArgs args) => new();

    protected TResult Failed(ToolError code) => new()
    {
        ErrorCodes = { code }
    };
}
