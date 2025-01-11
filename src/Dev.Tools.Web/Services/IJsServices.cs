namespace Dev.Tools.Web.Services;

public interface IJsServices
{
    ValueTask CopyToClipboardAsync(string text, CancellationToken token = default);
}