using System.Web;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "url-transcoder",
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
        Encode,
        Decode
    }

    public readonly record struct Args(
        [property: PipeInput] string Url,
        TranscodingType Transcoding
    );

    public sealed record Result([property: PipeOutput] string? Url) : ToolResult
    {
        public Result() : this(Url: null) { }
    }
}