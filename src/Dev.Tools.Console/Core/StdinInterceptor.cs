using System.Reflection;
using Spectre.Console.Cli;

namespace Dev.Tools.Console.Core;

public sealed class StdinInterceptor : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        // Find the property marked with [CommandArgument(0, ...)]
        var targetProp = settings
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => new
            {
                Property = p,
                Attribute = p.GetCustomAttribute<CommandArgumentAttribute>()
            })
            .Where(x => x.Attribute is { Position: 0 })
            .Select(x => x.Property)
            .FirstOrDefault();

        if (targetProp is null)
        {
            return;
        }

        // An argument given on the command line wins, and stdin is read only when it is missing. Reading
        // stdin regardless replaced the argument with whatever was piped (`echo x | dev-tools encrypt hello`
        // encrypted "x"), and hung wherever stdin is redirected but left open with nothing written to it -
        // CI, IDE run configurations, process launchers.
        var currentValue = targetProp.GetValue(settings) as string;
        if (!string.IsNullOrWhiteSpace(currentValue))
        {
            return;
        }

        if (System.Console.IsInputRedirected)
        {
            var stdin = System.Console.In.ReadToEnd();
            if (string.IsNullOrWhiteSpace(stdin))
            {
                return;
            }

            stdin = stdin.TrimEnd('\r', '\n');

            if (targetProp.PropertyType == typeof(string) && targetProp.CanWrite)
            {
                targetProp.SetValue(settings, stdin);
            }

            return;
        }

        throw new ArgumentException(
            $"Missing required input for argument '{targetProp.Name}'. " +
            "Provide argument or pipe data into the command.");
    }
  
    public void InterceptResult(CommandContext context, CommandSettings settings, ref int result)
    {
    }
}