using Dev.Tools.Core;
using System.Text;

namespace Dev.Tools.Tools;


[ToolDefinition(
    Name = "base64-decoder",
    Aliases = ["d64"]
    // [ToolKeyword.Base64, ToolKeyword.Decode, ToolKeyword.Url]
    // Categories = [ToolCategory.Converter],
    // ErrorCodes = [ToolError.Unknown, "TEXT_EMPTY"]
)]
public class Base64DecoderTool : ToolBase<Base64DecoderTool.Args, Base64DecoderTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            return Failed("TEXT_EMPTY");
        }

        byte[] bytes = Convert.FromBase64String(args.Text);
        string decoded = Encoding.UTF8.GetString(bytes);

        return new(decoded);
    }

    public record Args(string Text) : ToolArgs;

    public record Result(string Text) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}
