namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "base-encoding",
    Title = "Base64 Encoder/Decoder",
    Description =
        """
        Encodes text or a file as Base64, or decodes Base64 back to text.

        Text is encoded as its UTF-8 bytes and a file as its raw bytes, in the standard alphabet with
        padding, and a line break can be inserted every 76 characters. Decoding ignores whitespace and
        line breaks, and reads the decoded bytes as UTF-8. With UrlSafe both use the URL and filename
        alphabet of RFC 4648 - '-' and '_' in place of '+' and '/' - leaving the padding out when
        encoding and accepting it with or without when decoding.
        """,
    Usage =
        "Use this instead of encoding or decoding Base64 by hand. Decoded bytes are read as UTF-8, so a decoded binary file does not come back intact.",
    Aliases = ["base64"],
    Keywords = [Keyword.Base64, Keyword.Encode, Keyword.Decode, Keyword.Url, Keyword.Text, Keyword.String],
    Categories = [Category.Converter]
)]
public sealed class BaseEncodingTool : ToolBase<BaseEncodingTool.Args, BaseEncodingTool.Result>
{
    protected override async Task<Result> ExecuteAsync(Args args, CancellationToken cancellationToken)
    {
        // Overriding ExecuteAsync skips the check ToolBase makes before running a tool.
        cancellationToken.ThrowIfCancellationRequested();

        return args.Transcoding == TranscodingType.Decode
            ? await DecodeAsync(args, cancellationToken)
            : await EncodeAsync(args, cancellationToken);
    }

    private static async Task<Result> EncodeAsync(Args args, CancellationToken cancellationToken)
    {
        // The encoded text is built whole, so the content is read whole.
        byte[] bytes = args.Text is null ? [] : await args.Text.ReadAllBytesAsync(cancellationToken);
        if (bytes.Length == 0)
        {
            throw new ToolException(ErrorCode.TextEmpty, nameof(Args.Text));
        }

        var encoded = Convert.ToBase64String(
            bytes,
            args.InsertLineBreaks ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None);

        if (args.UrlSafe)
        {
            encoded = encoded
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        return new Result(encoded);
    }

    private static async Task<Result> DecodeAsync(Args args, CancellationToken cancellationToken)
    {
        var text = args.Text is null ? string.Empty : await args.Text.ReadAllTextAsync(cancellationToken);
        var base64 = string.Concat(text.Where(it => !char.IsWhiteSpace(it)));
        if (base64.Length == 0)
        {
            throw new ToolException(ErrorCode.TextEmpty, nameof(Args.Text));
        }

        if (args.UrlSafe)
        {
            base64 = base64.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        }

        try
        {
            return new Result(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));
        }
        catch (FormatException)
        {
            throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Text));
        }
    }

    public enum TranscodingType
    {
        [Description("Encode the input as Base64 text")]
        Encode,

        [Description("Decode Base64 text back to text")]
        Decode
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Text or file content to encode, text being encoded as UTF-8; or Base64 text to decode")]
        ToolContent Text,
        [property: DisplayName("Operation"),
                   Description("Whether to encode the input as Base64 or decode Base64 back to text")]
        TranscodingType Transcoding = TranscodingType.Encode,
        [property: DisplayName("URL Safe"),
                   Description("Use the URL-safe alphabet ('-' and '_' in place of '+' and '/'): encoding leaves out the padding, decoding accepts it with or without")]
        bool UrlSafe = false,
        [property: Description("Encoding only: insert a line break every 76 characters")]
        bool InsertLineBreaks = false
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("Result"),
                   Description("The Base64 text when encoding, or the decoded text when decoding")]
        string Text
    ) : ToolResult
    {
        public Result() : this(string.Empty)
        {
        }
    }
}
