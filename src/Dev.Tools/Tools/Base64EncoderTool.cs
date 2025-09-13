namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "base64-encoder",
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

    public record Args
    {
        public string? Text { get; set; }
        public bool InsertLineBreaks { get; set; }
        public bool UrlSafe { get; set; }
    }

    public record Result(string Text) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}
