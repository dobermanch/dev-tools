namespace Dev.Tools.Tools;

public abstract class ToolBase<TArgs, TResult> : ITool<TArgs, TResult>, IToolAsync<TArgs, TResult>
    where TArgs : ToolArgs
    where TResult: ToolResult, new()
{
    public TResult Run(TArgs args) => RunAsync(args, CancellationToken.None).GetAwaiter().GetResult();

    public async Task<TResult> RunAsync(TArgs args, CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteAsync(args, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return Failed(ErrorCode.Unknown);
        }
    }

    protected virtual Task<TResult> ExecuteAsync(TArgs args, CancellationToken cancellationToken)
    {
        return Task.FromResult(Execute(args));
    }

    protected virtual TResult Execute(TArgs args) => new();

    protected TResult Failed(ErrorCode code) => new ()
    {
        ErrorCodes = { code }
    };
}
