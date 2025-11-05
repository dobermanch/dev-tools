namespace Dev.Tools.Web.Services.JavaScript;

public interface IJsServices
{
    ValueTask CopyToClipboardAsync(string text, CancellationToken cancellationToken = default);
    
    ValueTask ScrollToIdAsync(string id, int offset = 0, CancellationToken cancellationToken = default);

    ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, params object?[]? args);

    ValueTask InvokeVoidAsync(string identifier, CancellationToken cancellationToken, params object?[]? args);
}