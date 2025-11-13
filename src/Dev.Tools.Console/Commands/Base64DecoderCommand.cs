using Dev.Tools.Console.Core;
using Dev.Tools.Core.Localization;
using Dev.Tools.Providers;
using Dev.Tools.Tools;
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

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        ToolDefinition definition = toolProvider.GetToolDefinition<Base64DecoderTool>();

        try
        {
            var tool = toolProvider.GetTool<Base64DecoderTool>();
            Base64DecoderTool.Args args = new Base64DecoderTool.Args
            {
                Text = settings.Text,
            };

            Base64DecoderTool.Result result = await tool.RunAsync(args, CancellationToken.None);
            
            return responseHandler.ProcessResponse(result, definition);
        }
        catch (Exception ex)
        {
            return responseHandler.ProcessError(ex, definition);

            //AnsiConsole.WriteException(ex);
            //return -1;
        }
    }
}