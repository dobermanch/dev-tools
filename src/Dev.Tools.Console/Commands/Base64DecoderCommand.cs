using Dev.Tools.Core;
using Dev.Tools.Core.Localization;
using Dev.Tools.Providers;
using Dev.Tools.Tools;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dev.Tools.Console.Commands;

internal sealed partial class Base64DecoderCommand(IToolsProvider toolProvider, IToolResponseHandler responseHandler) 
    : AsyncCommand<Base64DecoderCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [LocalizedDescription("Text to decode")]
        [CommandArgument(0, "[text]")]
        public string Text { get; init; } = default!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            var tool = toolProvider.GetTool<Base64DecoderTool>();
            Base64DecoderTool.Args args = new Base64DecoderTool.Args(
                settings.Text
            );

            PostUpdateArgs(ref args, context, settings);

            Base64DecoderTool.Result result = await tool.RunAsync(args, CancellationToken.None);

            return responseHandler.Handle(result);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return -1;
        }
    }

    partial void PostUpdateArgs(ref Base64DecoderTool.Args args, CommandContext context, Settings settings);
}

public interface IToolResponseHandler
{
    int Handle(ToolResult result);
}