using Dev.Tools.Console.Commands;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using Spectre.Console.Rendering;

namespace Dev.Tools.Console.Core;

/// <summary>
/// Shows a tool's full description in <c>dev-tools &lt;tool&gt; --help</c>.
///
/// Spectre reads a single <see cref="System.ComponentModel.DescriptionAttribute"/> for both the root
/// command list and the DESCRIPTION block of one command. The list has room for a line, so the
/// generated attribute carries only the first one; here there is room for all of it.
/// </summary>
internal sealed class ToolHelpProvider(ICommandAppSettings settings) : HelpProvider(settings)
{
    public override IEnumerable<IRenderable> GetDescription(ICommandModel model, ICommandInfo? command)
    {
        if (command?.Name is null
            || !ToolDescriptions.DetailedByCommandName.TryGetValue(command.Name, out var description))
        {
            return base.GetDescription(model, command);
        }

        return
        [
            new Text("DESCRIPTION:"),
            Text.NewLine,
            new Text(description),
            Text.NewLine,
            Text.NewLine,
        ];
    }
}
