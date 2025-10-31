namespace Dev.Tools.Web.Services;

public interface IJsServices
{
    ValueTask CopyToClipboardAsync(string text, CancellationToken token = default);
    ValueTask ScrollToIdAsync(string id, int offset = 0, CancellationToken token = default);
}