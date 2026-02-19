using System.Text.Json;
using Spectre.Console;

namespace Dev.Tools.Console.Core;

internal sealed class ToolResponseHandler : IToolResponseHandler
{
    public int ProcessResponse(ToolResult result, ToolDefinition definition, SettingsBase settings)
    {
        var outputProperty = definition.ReturnType.Properties.FirstOrDefault(it => it.IsPipeOutput);
        if (outputProperty != null)
        {
            var value = result.GetType().GetProperty(outputProperty.Name)!.GetValue(result, null);
            AnsiConsole.WriteLine(value?.ToString() ?? string.Empty);
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

    public int ProcessError(Exception exception, ToolDefinition definition, SettingsBase settings)
    {
        AnsiConsole.WriteException(exception);
        return -1;
    }
}