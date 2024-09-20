namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "base64-decoder",
    Aliases = ["64d"],
    Keywords = [Keyword.Base64, Keyword.Decode, Keyword.Url, Keyword.Text, Keyword.String],
    Categories = [Category.Converter],
    ErrorCodes = [Error.Unknown, Error.TextEmpty]
)]
public class Base64DecoderTool : ToolBase<Base64DecoderTool.Args, Base64DecoderTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            return Failed(Error.TextEmpty);
        }

        byte[] bytes = Convert.FromBase64String(args.Text);
        string decoded = Encoding.UTF8.GetString(bytes);

        return new(decoded);
    }

    public record Args(string Text) : ToolArgs;

    public record Result(string Data) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}
