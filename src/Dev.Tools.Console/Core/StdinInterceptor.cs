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

        var currentValue = targetProp.GetValue(settings) as string;
        if (string.IsNullOrWhiteSpace(currentValue))
        {
            throw new ArgumentException(
                $"Missing required input for argument '{targetProp.Name}'. " +
                "Provide argument or pipe data into the command.");
        }
    }
  
    public void InterceptResult(CommandContext context, CommandSettings settings, ref int result)
    {
    }
}