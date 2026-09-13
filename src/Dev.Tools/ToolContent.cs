using System.Runtime.InteropServices;
using System.Text;

namespace Dev.Tools;

/// <summary>
/// The data a tool works on when it can be more than a line of text: text, bytes, or a stream opened
/// when the tool reads it - a file, an upload, stdin. The tool reads it the way it needs to and does not
/// care which one it was given; how a user supplies a file is each frontend's business.
///
/// A string converts implicitly, so <c>new HashTextTool.Args("hello")</c> still compiles. A tool whose
/// input is a text document (a JSON formatter) takes a plain <c>string</c> instead: the frontends load
/// files for those too, and hand over the text.
/// </summary>
public sealed class ToolContent
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly string? _text;
    private readonly ReadOnlyMemory<byte> _bytes;
    private readonly Func<Stream>? _open;

    private ToolContent(string? text, ReadOnlyMemory<byte> bytes, Func<Stream>? open)
    {
        _text = text;
        _bytes = bytes;
        _open = open;
    }

    /// <summary>Text, read by the tool as UTF-8 bytes. No text is the same as empty text.</summary>
    public static ToolContent FromText(string? text) => new(text ?? string.Empty, default, null);

    public static ToolContent FromBytes(ReadOnlyMemory<byte> bytes) => new(null, bytes, null);

    /// <summary>
    /// A stream, opened each time the content is read and disposed by whoever reads it. A source that can
    /// only be read once, such as stdin, reads as empty the second time.
    /// </summary>
    public static ToolContent FromStream(Func<Stream> open)
        => new(null, default, open ?? throw new ArgumentNullException(nameof(open)));

    public static implicit operator ToolContent(string? text) => FromText(text);

    public Stream OpenRead()
    {
        if (_open is not null)
        {
            return _open();
        }

        if (_text is not null)
        {
            return new MemoryStream(Utf8.GetBytes(_text), writable: false);
        }

        return MemoryMarshal.TryGetArray(_bytes, out var segment)
            ? new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false)
            : new MemoryStream(_bytes.ToArray(), writable: false);
    }

    public async Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken)
    {
        if (_text is not null)
        {
            return Utf8.GetBytes(_text);
        }

        if (_open is null)
        {
            return _bytes.ToArray();
        }

        await using var stream = _open();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <summary>
    /// The content as text: UTF-8 unless a byte order mark says otherwise, with any invalid byte sequence
    /// replaced by U+FFFD rather than failing.
    /// </summary>
    public async Task<string> ReadAllTextAsync(CancellationToken cancellationToken)
    {
        if (_text is not null)
        {
            return _text;
        }

        await using var stream = OpenRead();
        using var reader = new StreamReader(stream, Utf8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }
}
