using System.Text.Json;
using Spectre.Console;

namespace Dev.Tools.Console.Core;

internal sealed class ToolResponseHandler : IToolResponseHandler
{
    private const int ToolFailedExitCode = 1;

    // Diagnostics go to stderr so piping tools together keeps stdout clean.
    private static readonly IAnsiConsole ErrorConsole = AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(System.Console.Error)
    });

    public int ProcessResponse(ToolResult result, ToolDefinition definition, SettingsBase settings)
    {
        if (result.HasErrors)
        {
            foreach (var errorCode in result.ErrorCodes)
            {
                ErrorConsole.MarkupLineInterpolated($"[red]{definition.Name} failed: {errorCode}[/]");
            }

            return ToolFailedExitCode;
        }

        var outputProperty = definition.ReturnType.Properties.FirstOrDefault(it => it.IsPipeOutput);
        if (outputProperty != null)
        {
            var value = result.GetType().GetProperty(outputProperty.Name)!.GetValue(result, null);
            AnsiConsole.WriteLine(FormatOutput(value));
        }
        else
        {
            AnsiConsole.WriteLine(JsonSerializer.Serialize(result, result.GetType(), new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }

        return 0;
    }

    // A collection output is one value per line, so piping it stays line-oriented
    // instead of printing the type name.
    private static string FormatOutput(object? value) => value switch
    {
        null => string.Empty,
        string text => text,
        System.Collections.IEnumerable items => string.Join(
            Environment.NewLine,
            items.Cast<object?>().Select(it => it?.ToString() ?? string.Empty)),
        _ => value.ToString() ?? string.Empty
    };

    public int ProcessError(Exception exception, ToolDefinition definition, SettingsBase settings)
    {
        ErrorConsole.WriteException(exception);
        return -1;
    }
}
