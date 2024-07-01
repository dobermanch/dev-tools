using Dev.Tools.Tools;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Dev.Tools.Console.Commands;

internal sealed class Base64DecoderCommand(Base64DecoderTool tool) : AsyncCommand<Base64DecoderCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Text to decode")]
        [CommandArgument(0, "[text]")]
        public string Text { get; init; } = default!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var args = new Base64DecoderTool.Args(
            settings.Text
        );

        Base64DecoderTool.Result result = await tool.RunAsync(args, CancellationToken.None);

        if (result.HasErrors)
        {
            AnsiConsole.WriteLine(result.ErrorCodes[0]);
            return -1;
        }

        AnsiConsole.WriteLine(result.Text);

        return 0;
    }
}