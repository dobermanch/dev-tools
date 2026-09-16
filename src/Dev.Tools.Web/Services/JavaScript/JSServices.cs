using Microsoft.JSInterop;

namespace Dev.Tools.Web.Services.JavaScript;

internal sealed class JsServices(IJSRuntime jsRuntime) : IJsServices
{
    public ValueTask CopyToClipboardAsync(string text, CancellationToken cancellationToken = default)
        => string.IsNullOrEmpty(text)
            ? default
            : jsRuntime.InvokeVoidAsync("devTools.copyToClipboard", cancellationToken, text);

    public ValueTask ScrollToIdAsync(string id, int offset = 0, CancellationToken cancellationToken = default)
        => string.IsNullOrWhiteSpace(id)
            ? ValueTask.CompletedTask
            : jsRuntime.InvokeVoidAsync("devTools.scrollToId", cancellationToken, id, offset);

    public ValueTask<string> ShareAsync(string title, string url, CancellationToken cancellationToken = default)
        => jsRuntime.InvokeAsync<string>("devTools.share", cancellationToken, title, url);

    public ValueTask DownloadAsync(string fileName, string text, string? contentType = null, CancellationToken cancellationToken = default)
        => string.IsNullOrEmpty(text)
            ? default
            : jsRuntime.InvokeVoidAsync("devTools.downloadFile", cancellationToken, fileName, text, contentType);

    public ValueTask SetThemeAsync(bool isDarkMode, CancellationToken cancellationToken = default)
        => jsRuntime.InvokeVoidAsync("devTools.setTheme", cancellationToken, isDarkMode);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, params object?[]? args)
        => jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);

    public ValueTask InvokeVoidAsync(string identifier, CancellationToken cancellationToken, params object?[]? args) 
        => jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
}