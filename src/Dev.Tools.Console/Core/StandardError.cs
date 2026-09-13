using Spectre.Console;

namespace Dev.Tools.Console.Core;

/// <summary>
/// Where diagnostics go: stderr, so piping tools together keeps stdout for results.
/// </summary>
internal static class StandardError
{
    public static readonly IAnsiConsole Console = AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(System.Console.Error)
    });
}
