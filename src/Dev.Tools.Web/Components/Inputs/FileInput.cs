using Microsoft.AspNetCore.Components.Forms;

namespace Dev.Tools.Web.Components.Inputs;

/// <summary>
/// Reading a file the user picked. Shared by <see cref="DtInputPane"/> and <see cref="DtTextField{T}"/>,
/// which both offer it while the pages move from fields to panes.
/// </summary>
internal static class FileInput
{
    /// <summary>
    /// The browser hands a file over once. Holding its bytes lets a page run the tool as often as it
    /// likes - again when an option changes, or once per algorithm.
    /// </summary>
    public static async Task<ToolContent> ReadAsync(IBrowserFile file, long limit, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream(limit);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return ToolContent.FromBytes(buffer.ToArray());
    }

    public static string FormatSize(long bytes)
        => bytes >= 1024 * 1024 ? $"{bytes / (1024 * 1024)} MB" : $"{bytes / 1024} KB";
}
