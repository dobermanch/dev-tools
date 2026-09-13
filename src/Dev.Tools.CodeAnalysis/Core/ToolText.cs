namespace Dev.Tools.CodeAnalysis.Core;

/// <summary>
/// Turning authored English into text that can be embedded in generated code.
/// </summary>
public static class ToolText
{
    /// <summary>
    /// Normalizes authored text to LF so generated code does not depend on the
    /// line endings of the source file it was read from.
    /// </summary>
    public static string Normalize(string? value)
        => value?.Replace("\r\n", "\n").Replace("\r", "\n") ?? string.Empty;

    /// <summary>
    /// Drops the line wrapping an author used to keep a raw string literal readable in the source
    /// file: a single newline inside a paragraph becomes a space, a blank line stays a paragraph
    /// break. Every consumer wraps text to its own width, so hard-wrapped source would wrap twice.
    /// </summary>
    public static string Unwrap(string? value)
    {
        var paragraphs = Normalize(value)
            .Split(["\n\n"], StringSplitOptions.None)
            .Select(paragraph => string.Join(
                " ",
                paragraph.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0)))
            .Where(paragraph => paragraph.Length > 0);

        return string.Join("\n\n", paragraphs);
    }

    /// <summary>
    /// First line of a description, for the places that have room for exactly one line:
    /// the CLI command list and the tool lists a client renders.
    /// </summary>
    public static string FirstLine(string value)
    {
        var end = value.IndexOf('\n');
        return end < 0 ? value : value.Substring(0, end).TrimEnd();
    }

    /// <summary>
    /// Escapes text for emission inside a C# string literal.
    /// </summary>
    public static string EscapeLiteral(string value)
        => value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
}
