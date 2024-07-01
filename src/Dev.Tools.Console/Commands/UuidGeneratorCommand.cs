using Dev.Tools.Tools;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Dev.Tools.Console.Commands;

internal sealed class UuidGeneratorCommand(UuidGeneratorTool tool) : AsyncCommand<UuidGeneratorCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("UUID version")]
        [CommandArgument(0, "[type]")]
        public UuidGeneratorTool.UuidType Type { get; init; }

        [Description("Number of UUID to generate")]
        [CommandOption("-c|--count <count>")]
        public int Count { get; init; } = 1;

        [Description("Namespace for V3|V5 UUID version")]
        [CommandOption("-n|--namespace <namesapce>")]
        public Guid? Namespace { get; init; }

        [Description("Name for V3|V5 UUID version")]
        [CommandOption("-N|--name <name>")]
        public string? Name { get; init; }

        [Description("Timestamp (ISO 8601) for V7 UUID version")]
        [CommandOption("-t|--time <time>")]
        public DateTimeOffset? Time { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var args = new UuidGeneratorTool.Args(
            settings.Type,
            settings.Count,
            settings.Namespace,
            settings.Name,
            settings.Time
        );

        UuidGeneratorTool.Result result = await tool.RunAsync(args, CancellationToken.None);

        if (result.HasErrors)
        {
            AnsiConsole.WriteLine(result.ErrorCodes[0]);
            return -1;
        }

        foreach (var uuid in result.Uuids)
        {
            AnsiConsole.WriteLine(uuid.ToString());
        }

        return 0;
    }
}