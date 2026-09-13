using System.Collections.Immutable;
using System.Text;
using Dev.Tools.CodeAnalysis.Core;
using Dev.Tools.Console.Core;
using Microsoft.CodeAnalysis;

namespace Dev.Tools.CodeAnalysis.Generators;

[Generator]
public class CliCommandGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
        => ToolPipeline.RegisterFrontend<GenerateToolsCliCommandAttribute>(context, "CLI command", Generate);

    private static void Generate(SourceProductionContext spc, ImmutableArray<ToolModel> tools)
    {
        var commands = new List<(string TypeName, string ToolName)>();
        foreach (var tool in tools)
        {
            var code = GenerateCliCommand(tool);
            commands.Add((code.TypeName, tool.Name));
            spc.AddSource(code.OutputFileName, code);
        }

        var registration = GenerateCommandRegistration(commands);
        spc.AddSource(registration.OutputFileName, registration);

        var descriptions = GenerateDetailedDescriptions(tools);
        spc.AddSource(descriptions.OutputFileName, descriptions);
    }

    /// <summary>
    /// The full text of the tools whose description runs to more than one line. Spectre reads one
    /// description for both the command list and a single command's help, and the list only has room
    /// for a line, so the help provider reads the detail from here instead.
    /// </summary>
    private static CodeBlock GenerateDetailedDescriptions(ImmutableArray<ToolModel> tools)
    {
        var detailed = tools
            .Where(it => it.Description.Contains('\n'))
            .Select(it => $"[\"{it.Name}\"] = \"{ToolText.EscapeLiteral(it.Description)}\",")
            .ToArray();

        return new CodeBlock
        {
            Namespace = "Dev.Tools.Console.Commands",
            TypeName = "ToolDescriptions",
            GeneratorType = typeof(CliCommandGenerator),
            Placeholders = new()
            {
                ["Entries"] = string.Join("\n        ", detailed)
            },
            Usings = ["System.Collections.Generic"],
            Content =
              """
              internal static class ToolDescriptions
              {
                  public static readonly IReadOnlyDictionary<string, string> DetailedByCommandName =
                      new Dictionary<string, string>
                      {
                          {Entries}
                      };
              }
              """
        };
    }

    private static CodeBlock GenerateCommandRegistration(List<(string TypeName, string ToolName)> commands)
        => new()
        {
            Namespace = "Dev.Tools.Console.Commands",
            TypeName = "CommandConfigurator",
            GeneratorType = typeof(CliCommandGenerator),
            Placeholders = new()
            {
                ["AddCommand"] = string.Join("\n        ",
                    commands.Select(it => $"ConfigureCommand<{it.TypeName}>(services, configurator, \"{it.ToolName}\");"))
            },
            Usings = [
                "Microsoft.Extensions.DependencyInjection",
                "Spectre.Console.Cli"
            ],
            Content = 
              """
              internal static class CommandConfigurator
              {
                  public static void ConfigureCommands(IServiceCollection services, IConfigurator configurator)
                  {
                      {AddCommand}
                  }
                  
                  private static void ConfigureCommand<T>(IServiceCollection services, IConfigurator configurator, string name)
                      where T : class, ICommand
                  {
                      configurator.AddCommand<T>(name);
                      services.AddTransient<T>();
                      services.AddTransient<ICommand, T>(provider => provider.GetRequiredService<T>());
                  }
              }
              """
        };

    private static CodeBlock GenerateCliCommand(ToolModel tool) =>
        new()
        {
            Namespace = "Dev.Tools.Console.Commands",
            TypeName = ToolNaming.CommandType(tool.TypeName),
            GeneratorType = typeof(CliCommandGenerator),
            Placeholders = new()
            {
                ["ToolName"] = tool.Name,
                ["ToolType"] = tool.TypeName,
                // Spectre shows this both as the DESCRIPTION block and as the row in the
                // root command list, so only the first line fits.
                ["ToolDescription"] = ToolText.EscapeLiteral(ToolText.FirstLine(tool.Description)),
                ["ToolArgsType"] = tool.Args.Type,
                ["ToolResultType"] = tool.Result.Type,
                ["SettingsType"] = GenerateSettingsClass(tool),
                ["SettingsMapping"] = ToolEmitter.Mapping(
                    tool.Args, $"{tool.TypeName}.Args", "args", it => $"settings.{it.Name}"),
            },
            Usings = [
                "System.ComponentModel",
                "Dev.Tools.Console.Core",
                "Dev.Tools.Providers",
                "Dev.Tools.Tools",
                "Spectre.Console",
                "Spectre.Console.Cli",
            ],
            Content = 
              """
              [Description("{ToolDescription}")]
              internal sealed partial class {TypeName}(Dev.Tools.Providers.IToolsProvider toolProvider, IToolResponseHandler responseHandler) 
                  : AsyncCommand<{TypeName}.Settings>
              {
                  {SettingsType}
                  
                  protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
                  {
                      ToolDefinition definition = toolProvider.GetToolDefinition<{ToolType}>();
                      
                      try
                      {
                          var tool = toolProvider.GetTool<{ToolType}>();
                          
                          {SettingsMapping}
                      
                          var result = await tool.RunAsync(args, cancellationToken);
                          
                          return responseHandler.ProcessResponse(result, definition, settings);
                      }
                      catch (Exception ex)
                      {
                          return responseHandler.ProcessError(ex, definition, settings);
                      }
                  }
              }
              """
        };
    
    private static string GenerateSettingsClass(ToolModel tool)
    {
        var properties = new StringBuilder();
        var shortNames = new HashSet<string>();

        foreach (var property in tool.Args.Properties)
        {
            var description = ToolText.EscapeLiteral(property.Description);

            // An option the user did not pass must keep the Args record's own default,
            // otherwise every unspecified option silently becomes default(T).
            // The property initializer is not enough on its own: Spectre binds absent
            // flags to false, and only honours the default through DefaultValueAttribute.
            var initializer = property.DefaultValue ?? "default!";
            var defaultAttribute = property.DefaultValue is null or "null"
                ? string.Empty
                : $"[System.ComponentModel.DefaultValue({property.DefaultValue})]\n            ";

            var letter = property.Name[0].ToString().ToLowerInvariant();
            var template = letter + property.Name.Substring(1);
            if (property.IsPipeInput)
            {
                // NOTE: use optional argument template [argument] instead of <argument> to support StdIn
                properties.AppendLine($$"""
                                                [Description("{{description}}")]
                                                [CommandArgument(0, "[{{template}}]")]
                                                public {{property.Type}} {{property.Name}} { get; set; } = {{initializer}};

                                        """);
            }
            else
            {
                template = $"--{template}";
                if (shortNames.Add(letter) || shortNames.Add(letter = letter.ToUpperInvariant()))
                {
                    template = $"-{letter}|{template}";
                }

                properties.AppendLine($$"""
                                                [Description("{{description}}")]
                                                [CommandOption("{{template}}")]
                                                {{defaultAttribute}}public {{property.Type}} {{property.Name}} { get; set; } = {{initializer}};

                                        """);
            }
        }

        return $$"""
                 public sealed class Settings : SettingsBase
                     {
                 {{properties}}    }
                 """;
    }
}
