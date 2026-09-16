using Microsoft.JSInterop;

namespace Dev.Tools.Web.Services.JavaScript;

public interface IJsServices
{
    ValueTask CopyToClipboardAsync(string text, CancellationToken cancellationToken = default);
    
    ValueTask ScrollToIdAsync(string id, int offset = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls the named method on <paramref name="reference"/> when Ctrl+K or Cmd+K is pressed, and
    /// answers whether this is a Mac, so the shortcut can be labelled the way that keyboard has it.
    /// </summary>
    ValueTask<bool> RegisterCommandShortcutAsync<T>(DotNetObjectReference<T> reference, string methodName, CancellationToken cancellationToken = default)
        where T : class;

    ValueTask UnregisterCommandShortcutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Offers the link through the system share sheet where there is one, and copies it otherwise.
    /// Answers with what happened: "shared", "cancelled", "copied" or "failed".
    /// </summary>
    ValueTask<string> ShareAsync(string title, string url, CancellationToken cancellationToken = default);

    /// <summary>Hands the text to the browser as a file to save.</summary>
    ValueTask DownloadAsync(string fileName, string text, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stamps the theme on the document, so the design tokens in tokens.css match MudBlazor's own theme.
    /// </summary>
    ValueTask SetThemeAsync(bool isDarkMode, CancellationToken cancellationToken = default);

    ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, params object?[]? args);

    ValueTask InvokeVoidAsync(string identifier, CancellationToken cancellationToken, params object?[]? args);
}