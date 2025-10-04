using Microsoft.JSInterop;

namespace Dev.Tools.Web.Services;

internal sealed class JsServices(IJSRuntime jsRuntime) : IJsServices
{
    public ValueTask CopyToClipboardAsync(string text, CancellationToken token = default)
    {
        return string.IsNullOrEmpty(text) ? default : jsRuntime.InvokeVoidAsync("copyToClipboard", token, text);
    }
}