namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "base64-decoder",
    Aliases = ["64d"],
    Keywords = [Keywords.Base64, Keywords.Decode, Keywords.Url, Keywords.Text, Keywords.String],
    Categories = [Categories.Converter],
    ErrorCodes = [ErrorCodes.Unknown, ErrorCodes.TextEmpty]
)]
public sealed class Base64DecoderTool : ToolBase<Base64DecoderTool.Args, Base64DecoderTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            return Failed(ErrorCodes.TextEmpty);
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
