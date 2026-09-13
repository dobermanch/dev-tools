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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation is the caller's decision, not a tool failure.
            throw;
        }
        catch (Exception)
        {
            return Failed1(ErrorCode.Unknown);
        }
    }

    protected virtual Task<TResult> ExecuteAsync(TArgs args, CancellationToken cancellationToken)
    {
        // An already-cancelled call does no work, whichever tool it is. The token used to be dropped
        // here for every synchronous tool.
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Execute(args, cancellationToken));
    }

    /// <summary>
    /// Override this rather than <see cref="Execute(TArgs)"/> when the work is long enough to be worth
    /// stopping part way - pass the token on to the loops that do it.
    /// </summary>
    protected virtual TResult Execute(TArgs args, CancellationToken cancellationToken)
        => Execute(args);

    protected virtual TResult Execute(TArgs args) 
        => throw new NotImplementedException();

    protected TResult Failed1(ErrorCode code) => new ()
    {
        ErrorCodes = { code }
    };
}
