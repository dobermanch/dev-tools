using Dev.Tools.Tools;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Dev.Tools.Console.Commands;

internal sealed class Base64EncoderCommand(Base64EncoderTool tool) : AsyncCommand<Base64EncoderCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Text to encode")]
        [CommandArgument(0, "[text]")]
        public string Text { get; init; } = default!;

        [Description("Insert line breaks")]
        [CommandOption("-i|--insertLineBreaks")]
        public bool InsertLineBreaks { get; init; }

        [Description("Encode URL safe")]
        [CommandOption("-u|--urlSafe")]
        public bool UrlSafe { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var args = new Base64EncoderTool.Args(
            settings.Text,
            settings.InsertLineBreaks,
            settings.UrlSafe
        );

        Base64EncoderTool.Result result = await tool.RunAsync(args, CancellationToken.None);

        if (result.HasErrors)
        {
            AnsiConsole.WriteLine(result.ErrorCodes[0]);
            return -1;
        }

        AnsiConsole.WriteLine(result.Text);

        return 0;
    }
}