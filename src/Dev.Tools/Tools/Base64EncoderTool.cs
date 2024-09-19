﻿namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "base64-encoder",
    Aliases = ["64e"],
    Keywords = [ToolConstants.Keyword.Base64, ToolConstants.Keyword.Encode, ToolConstants.Keyword.Url],
    Categories = [ToolConstants.Category.Converter],
    ErrorCodes = [ToolConstants.Error.Unknown, ToolConstants.Error.TextEmpty]
)]
public class Base64EncoderTool : ToolBase<Base64EncoderTool.Args, Base64EncoderTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            return Failed(ToolConstants.Error.TextEmpty);
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

    public record Args(string Text, bool InsertLineBreaks = false, bool UrlSafe = false) : ToolArgs;

    public record Result(string Text) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}
