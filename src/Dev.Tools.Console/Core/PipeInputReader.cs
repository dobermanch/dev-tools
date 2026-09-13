using System.Text;

namespace Dev.Tools.Console.Core;

/// <summary>
/// Supplies a command's [PipeInput] argument: the value given on the command line when there is one,
/// otherwise whatever was piped or redirected into stdin (<c>dev-tools hash &lt; file.iso</c>).
///
/// A given argument always wins, and stdin is not touched then. Reading stdin regardless - which the
/// interceptor this replaces did - replaced the argument with piped data, and hung wherever stdin is
/// redirected but never written to: CI, IDE run configurations, process launchers.
/// </summary>
internal sealed class PipeInputReader(Func<bool>? isInputRedirected = null, Func<Stream>? openStandardInput = null)
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly Func<bool> _isInputRedirected = isInputRedirected ?? (() => System.Console.IsInputRedirected);
    private readonly Func<Stream> _openStandardInput = openStandardInput ?? System.Console.OpenStandardInput;

    /// <summary>
    /// For a tool that takes a string. Stdin is read whole, and its trailing line break dropped:
    /// <c>echo text | dev-tools ...</c> sends one that nobody meant as part of the text.
    /// </summary>
    public string ReadText(string? argument)
    {
        if (IsGiven(argument) || !_isInputRedirected())
        {
            return argument ?? string.Empty;
        }

        using var reader = new StreamReader(_openStandardInput(), Utf8, detectEncodingFromByteOrderMarks: true);
        var stdin = reader.ReadToEnd();

        return string.IsNullOrWhiteSpace(stdin)
            ? argument ?? string.Empty
            : stdin.TrimEnd('\r', '\n');
    }

    /// <summary>
    /// For a tool that takes <see cref="ToolContent"/>. Stdin is streamed as it is, trailing line break
    /// included: content is data, and the hash of a file has to be the hash of that file.
    /// </summary>
    public ToolContent ReadContent(string? argument)
    {
        if (IsGiven(argument) || !_isInputRedirected())
        {
            return ToolContent.FromText(argument);
        }

        return ToolContent.FromStream(_openStandardInput);
    }

    private static bool IsGiven(string? argument) => !string.IsNullOrWhiteSpace(argument);
}
