namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "base64-encoder",
    Title = "Base64 Encoder",
    Description =
        "Encodes text or a file to Base64 format with optional URL-safe encoding and line breaks for compatibility with various systems. Text is encoded as UTF-8 bytes, a file as its raw bytes.",
    Aliases = ["64e"],
    Keywords = [Keyword.Base64, Keyword.Encode, Keyword.Url, Keyword.Text, Keyword.String],
    Categories = [Category.Converter]
)]
public sealed class Base64EncoderTool : ToolBase<Base64EncoderTool.Args, Base64EncoderTool.Result>
{
    protected override async Task<Result> ExecuteAsync(Args args, CancellationToken cancellationToken)
    {
        // Overriding ExecuteAsync skips the check ToolBase makes before running a tool.
        cancellationToken.ThrowIfCancellationRequested();

        // The encoded text is built whole, so the content is read whole.
        byte[] bytes = args.Text is null ? [] : await args.Text.ReadAllBytesAsync(cancellationToken);
        if (bytes.Length == 0)
        {
            throw new ToolException(ErrorCode.TextEmpty, nameof(Args.Text));
        }

        string encoded = Convert.ToBase64String(
            bytes,
            args.InsertLineBreaks
                ? Base64FormattingOptions.InsertLineBreaks
                : Base64FormattingOptions.None
        );

        if (args.UrlSafe)
        {
            encoded = encoded
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        return new(encoded);
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Text or file content to encode; text is encoded as UTF-8")]
        ToolContent Text,
        [property: Description("Insert line breaks every 76 characters for better readability")]
        bool InsertLineBreaks = false,
        [property: DisplayName("URL Safe"),
                   Description("Use URL-safe encoding (replaces + with -, / with _, and removes padding)")]
        bool UrlSafe = false
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("Encoded Text"),
                   Description("The Base64-encoded text")]
        string Text
    ) : ToolResult
    {
        public Result() : this(string.Empty)
        {
        }
    }
}
