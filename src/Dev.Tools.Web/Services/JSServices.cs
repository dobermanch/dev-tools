using Microsoft.JSInterop;

namespace Dev.Tools.Web.Services;

internal sealed class JsServices(IJSRuntime jsRuntime) : IJsServices
{
    public ValueTask CopyToClipboardAsync(string text, CancellationToken token = default)
        => string.IsNullOrEmpty(text)
            ? default
            : jsRuntime.InvokeVoidAsync("devTools.copyToClipboard", token, text);

    public ValueTask ScrollToIdAsync(string id, int offset = 0, CancellationToken token = default)
        => string.IsNullOrWhiteSpace(id)
            ? ValueTask.CompletedTask
            : jsRuntime.InvokeVoidAsync("devTools.scrollToId", token, id, offset);
}