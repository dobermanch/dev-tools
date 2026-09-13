using System.Globalization;
using System.Net;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "html-entities",
    Title = "HTML Entities Encoder/Decoder",
    Description =
        """
        Escapes text for HTML, or turns HTML character references back into text.

        Encoding replaces the five characters HTML gives a meaning to - &, <, >, " and ' - with &amp;,
        &lt;, &gt;, &quot; and &#39;, which is what text needs to sit safely inside an element or a
        quoted attribute. With EncodeNonAscii every character outside ASCII becomes a decimal numeric
        reference too (é becomes &#233;), for a page whose encoding cannot hold it.

        Decoding understands decimal and hexadecimal numeric references and the named entities of
        HTML 4, such as &nbsp;, &copy; and &eacute;, plus &apos;. A reference without its closing
        semicolon, or one it does not recognise, is left as it is.
        """,
    Aliases = [],
    Keywords = [Keyword.Html, Keyword.Encode, Keyword.Decode, Keyword.Web, Keyword.Text, Keyword.String],
    Categories = [Category.Web]
)]
public sealed class HtmlEntitiesTool : ToolBase<HtmlEntitiesTool.Args, HtmlEntitiesTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            throw new ToolException(ErrorCode.TextEmpty, nameof(Args.Text));
        }

        return new Result(args.Transcoding == TranscodingType.Encode
            ? Encode(args.Text, args.EncodeNonAscii)
            : WebUtility.HtmlDecode(args.Text));
    }

    private static string Encode(string text, bool encodeNonAscii)
    {
        var builder = new StringBuilder(text.Length);
        Span<char> units = stackalloc char[2];

        foreach (var rune in text.EnumerateRunes())
        {
            switch (rune.Value)
            {
                case '&':
                    builder.Append("&amp;");
                    break;
                case '<':
                    builder.Append("&lt;");
                    break;
                case '>':
                    builder.Append("&gt;");
                    break;
                case '"':
                    builder.Append("&quot;");
                    break;
                case '\'':
                    builder.Append("&#39;");
                    break;
                case > 0x7F when encodeNonAscii:
                    builder.Append("&#").Append(rune.Value.ToString(CultureInfo.InvariantCulture)).Append(';');
                    break;
                default:
                    builder.Append(units[..rune.EncodeToUtf16(units)]);
                    break;
            }
        }

        return builder.ToString();
    }

    public enum TranscodingType
    {
        [Description("Escape the characters HTML gives a meaning to")]
        Encode,

        [Description("Turn character references back into text")]
        Decode
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Text to escape, or HTML whose character references to decode")]
        string? Text,
        [property: DisplayName("Operation"),
                   Description("Whether to escape the text or decode the references in it")]
        TranscodingType Transcoding = TranscodingType.Encode,
        [property: DisplayName("Encode Non-ASCII"),
                   Description("When encoding, also write every character outside ASCII as a numeric reference, such as &#233; for é; ignored when decoding")]
        bool EncodeNonAscii = false
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("Result"),
                   Description("The escaped text, or the text with its references decoded")]
        string Text
    ) : ToolResult
    {
        public Result() : this(string.Empty)
        {
        }
    }
}
