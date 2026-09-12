using System.Web;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "url-transcoder",
    Title = "URL Encoder/Decoder",
    Description =
        "Encodes or decodes URL text by converting special characters to percent-encoded form. Encoding follows the form-encoding rules, so a space becomes '+' rather than '%20'.",
    Aliases = [],
    Keywords = [Keyword.Decode, Keyword.Encode, Keyword.Url, Keyword.Text, Keyword.String],
    Categories = [Category.Converter]
)]
public sealed class SafeUrlTranscoderTool : ToolBase<SafeUrlTranscoderTool.Args, SafeUrlTranscoderTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Url))
        {
            throw new ToolException(ErrorCode.TextEmpty);
        }

        string url = args.Transcoding == TranscodingType.Encode
            ? HttpUtility.UrlEncode(args.Url)
            : HttpUtility.UrlDecode(args.Url);
        
        return new(url);
    }

    public enum TranscodingType
    {
        [Description("Percent-encode the input, writing spaces as '+'")]
        Encode,

        [Description("Decode percent-encoded input")]
        Decode
    }

    public sealed record Args(
        [property: PipeInput,
                   DisplayName("URL"),
                   Description("URL or text to encode or decode")]
        string Url,
        [property: DisplayName("Operation"),
                   Description("Whether to percent-encode the input or decode it")]
        TranscodingType Transcoding
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("Result"),
                   Description("The encoded or decoded URL")]
        string? Url
    ) : ToolResult
    {
        public Result() : this(Url: null) { }
    }
}