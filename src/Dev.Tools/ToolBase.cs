namespace Dev.Tools;

public abstract class ToolBase<TArgs, TResult> : ITool<TArgs, TResult>
    where TResult: ToolResult, new()
{
    public async Task<TResult> RunAsync(TArgs args, CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteAsync(args, cancellationToken).ConfigureAwait(false);
        }
        catch (ToolException ex)
        {
            return Failed1(ex.ErrorCode);
        }
        catch (Exception)
        {
            return Failed1(ErrorCode.Unknown);
        }
    }

    protected virtual Task<TResult> ExecuteAsync(TArgs args, CancellationToken cancellationToken) 
        => Task.FromResult(Execute(args));

    protected virtual TResult Execute(TArgs args) 
        => throw new NotImplementedException();

    protected TResult Failed1(ErrorCode code) => new ()
    {
        ErrorCodes = { code }
    };
}
