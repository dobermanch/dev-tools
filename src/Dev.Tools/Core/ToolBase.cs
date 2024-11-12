namespace Dev.Tools.Core;

public abstract class ToolBase<TArgs, TResult> : ITool<TArgs, TResult>, IToolAsync<TArgs, TResult>
    where TArgs : ToolArgs
    where TResult: ToolResult, new()
{
    public TResult Run(TArgs args)
    {
        throw new NotImplementedException();
    }

    public async Task<TResult> RunAsync(TArgs args, CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteAsync(args, cancellationToken);
        }
        catch (Exception ex)
        {
            return Failed(ErrorCodes.Unknown);
        }
    }

    protected virtual Task<TResult> ExecuteAsync(TArgs args, CancellationToken cancellationToken)
    {
        return Task.FromResult(Execute(args));
    }

    protected virtual TResult Execute(TArgs args) => new();

    protected TResult Failed(ErrorCodes code) => new ()
    {
        ErrorCodes = { code }
    };
}
