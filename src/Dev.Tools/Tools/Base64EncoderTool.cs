namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "base64-encoder",
    Title = "Base64 Encoder",
    Description =
        "Encodes text to Base64 format with optional URL-safe encoding and line breaks for compatibility with various systems",
    Aliases = ["64e"],
    Keywords = [Keyword.Base64, Keyword.Encode, Keyword.Url, Keyword.Text, Keyword.String],
    Categories = [Category.Converter]
)]
public sealed class Base64EncoderTool : ToolBase<Base64EncoderTool.Args, Base64EncoderTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            throw new ToolException(ErrorCode.TextEmpty);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(args.Text);

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
                   DisplayName("Text"),
                   Description("Text to encode to Base64 format")]
        string Text,
        [property: DisplayName("Insert Line Breaks"),
                   Description("Insert line breaks every 76 characters for better readability")]
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