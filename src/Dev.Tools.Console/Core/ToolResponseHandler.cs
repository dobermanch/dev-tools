using System.Text.Json;
using Spectre.Console;

namespace Dev.Tools.Console.Core;

internal sealed class ToolResponseHandler(TextWriter? output = null) : IToolResponseHandler
{
    // Where results go: stdout, unless a test captures them.
    private readonly TextWriter _output = output ?? System.Console.Out;

    private const int ToolFailedExitCode = 1;

    // The shell convention for a command interrupted by Ctrl+C.
    private const int CancelledExitCode = 130;

    // Diagnostics go to stderr so piping tools together keeps stdout clean.
    private static readonly IAnsiConsole ErrorConsole = AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(System.Console.Error)
    });

    public int ProcessResponse(ToolResult result, ToolDefinition definition, SettingsBase settings)
    {
        if (result.HasErrors)
        {
            foreach (var error in result.Errors)
            {
                ErrorConsole.MarkupLineInterpolated($"[red]{Describe(definition, error)}[/]");
            }

            return ToolFailedExitCode;
        }

        // The result goes to stdout as it is, not through Spectre: Spectre wraps text to the console width
        // (80 columns when redirected), which put a newline every 80 characters into anything piped on -
        // `base64-encoder < file | base64-decoder` did not give the file back.
        var outputProperty = definition.ReturnType.Properties.FirstOrDefault(it => it.IsPipeOutput);
        if (outputProperty != null)
        {
            var value = result.GetType().GetProperty(outputProperty.Name)!.GetValue(result, null);
            _output.WriteLine(FormatOutput(value));
        }
        else
        {
            _output.WriteLine(JsonSerializer.Serialize(result, result.GetType(), new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }

        return 0;
    }

    /// <summary>
    /// "text-encryptor: --key: Input is not valid". The target is spelled the way it is typed: the
    /// positional pipe-input argument by its name, every other argument as its long option.
    /// </summary>
    private static string Describe(ToolDefinition definition, ToolError error)
    {
        if (error.Target is null)
        {
            return $"{definition.Name}: {error.Message}";
        }

        var name = char.ToLowerInvariant(error.Target[0]) + error.Target[1..];
        var isPositional = definition.ArgsType.Properties.Any(it => it.Name == error.Target && it.IsPipeInput);

        return $"{definition.Name}: {(isPositional ? name : "--" + name)}: {error.Message}";
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

    /// <summary>
    /// A failure outside the tool itself - a tool turns its own exceptions into errors - so nothing in the
    /// exception is something the user can act on. The full stack trace used to be printed here.
    /// </summary>
    public int ProcessError(Exception exception, ToolDefinition definition, SettingsBase settings)
    {
        if (exception is OperationCanceledException)
        {
            ErrorConsole.MarkupLineInterpolated($"[yellow]{definition.Name}: cancelled[/]");
            return CancelledExitCode;
        }

        ErrorConsole.MarkupLineInterpolated($"[red]{definition.Name}: the tool failed unexpectedly[/]");
        return -1;
    }
}
