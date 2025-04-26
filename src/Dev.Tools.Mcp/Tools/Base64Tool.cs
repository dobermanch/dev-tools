using System.ComponentModel;
using Dev.Tools.Providers;
using Dev.Tools.Tools;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class Base64Tool
{
    [McpServerTool, Description("Decodes base64 encoded data.")]
    public static async Task<Base64DecoderTool.Result> Decode1(
        IToolsProvider tools,
        [Description("The base64 string to decode")]
        Base64DecoderTool.Args args,
        CancellationToken cancellationToken)
    {
        Base64DecoderTool.Result result = await tools
            .GetTool<Base64DecoderTool>()
            .RunAsync(
                args,
                //new Base64DecoderTool.Args { Text = input },
                cancellationToken
            );
        return result;
    }

    [McpServerTool, Description("Encodes string to base64 string.")]
    public static async Task<Base64EncoderTool.Result> Encode1(
        IToolsProvider tools,
        [Description("The string to encode to base64 string.")]
        Base64EncoderTool.Args args,
        CancellationToken cancellationToken)
    {
        Base64EncoderTool.Result result = await tools
                .GetTool<Base64EncoderTool>()
                .RunAsync(
                    args,
                    //new Base64EncoderTool.Args { Text = input },
                    cancellationToken
                );
        return result;
    }
}
