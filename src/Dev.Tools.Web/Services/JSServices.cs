using Microsoft.JSInterop;

namespace Dev.Tools.Web.Services;

internal sealed class JsServices(IJSRuntime jsRuntime) : IJsServices
{
    public ValueTask CopyToClipboardAsync(string text, CancellationToken token = default)
    {
        return jsRuntime.InvokeVoidAsync("copyToClipboard", token, text);
    }
}