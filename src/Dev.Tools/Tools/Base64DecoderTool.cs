namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "base64-decoder",
    Title = "Base64 Decoder",
    Description =
        "Decodes Base64-encoded text back to its original string representation, reading the decoded bytes as UTF-8. Expects the standard Base64 alphabet with padding.",
    Aliases = ["64d"],
    Keywords = [Keyword.Base64, Keyword.Decode, Keyword.Url, Keyword.Text, Keyword.String],
    Categories = [Category.Converter]
)]
public sealed class Base64DecoderTool : ToolBase<Base64DecoderTool.Args, Base64DecoderTool.Result>
{
    protected override Result Execute(Args args)
    {
        try
        {
            if (string.IsNullOrEmpty(args.Text))
            {
                throw new ToolException(ErrorCode.TextEmpty);
            }

            byte[] bytes = Convert.FromBase64String(args.Text);
            string decoded = Encoding.UTF8.GetString(bytes);

            return new(decoded);
        }
        catch (FormatException)
        {
            throw new ToolException(ErrorCode.InputNotValid);
        }
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Base64-encoded text to decode; must use the standard alphabet and be correctly padded")]
        string Text
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("Decoded Text"),
                   Description("The decoded text in its original format")]
        string Data
    ) : ToolResult
    {
        public Result() : this(string.Empty)
        {
        }
    }
}