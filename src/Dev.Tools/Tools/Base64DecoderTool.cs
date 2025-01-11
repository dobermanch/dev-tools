namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "base64-decoder",
    Aliases = ["64d"],
    Keywords = [Keyword.Base64, Keyword.Decode, Keyword.Url, Keyword.Text, Keyword.String],
    Categories = [Category.Converter],
    ErrorCodes = [ErrorCode.Unknown, ErrorCode.TextEmpty, ErrorCode.InputNotValid]
)]
public sealed class Base64DecoderTool : ToolBase<Base64DecoderTool.Args, Base64DecoderTool.Result>
{
    protected override Result Execute(Args args)
    {
        try
        {
            if (string.IsNullOrEmpty(args.Text))
            {
                return Failed(ErrorCode.TextEmpty);
            }

            byte[] bytes = Convert.FromBase64String(args.Text);
            string decoded = Encoding.UTF8.GetString(bytes);

            return new(decoded);
        }
        catch (FormatException)
        {
            return Failed(ErrorCode.InputNotValid);
        }
    }

    public record Args : ToolArgs
    {
        public string? Text { get; set; }
    }

    public record Result(string Data) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}
