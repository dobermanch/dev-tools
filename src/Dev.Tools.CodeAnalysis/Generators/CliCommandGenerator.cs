using System.Collections.Immutable;
using Dev.Tools.CodeAnalysis.Core;
using Dev.Tools.Console.Core;
using Microsoft.CodeAnalysis;

namespace Dev.Tools.CodeAnalysis.Generators;

[Generator]
public class CliCommandGenerator : ToolGeneratorBase, IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var referencedToolTypes =
            context.CompilationProvider
                .SelectMany((compilation, _) =>
                    compilation
                        .References
                        .Select(compilation.GetAssemblyOrModuleSymbol)
                        .OfType<IAssemblySymbol>()
                        .Where(it => it.Name == CodeDefinitions.ToolsAssemblyName)
                        .Select(it => it.GlobalNamespace)
                        .SelectMany(it => it.GetAllTypes(t => t.HasAttribute(CodeDefinitions.ToolDefinitionAttribute.TypeFullName)))
                        .Select(it => GetToolDetails((INamedTypeSymbol)it))
                        .Where(it => it is not null)
                        .Select(it => it!)
                );

        var attributes = GetAssemblyAttribute<GenerateToolsCliCommandAttribute>(context.CompilationProvider);

        var toolsAndAttribute = referencedToolTypes
            .Collect()
            .Combine(attributes);

        context.RegisterSourceOutput(toolsAndAttribute, (spc, data) =>
        {
            if (data.Right is null)
            {
                return; // Don't generate if attribute not present
            }

            GenerateTools(spc, data.Left);
        });
    }

    protected override void GenerateTools(SourceProductionContext spc, ImmutableArray<ToolDetails> tools)
    {
        if (tools.Length <= 0)
        {
            spc.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "DTG1002",
                        "The tools not found by CLI Generator",
                        "The CLI generator did not found any tools",
                        "Generator",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None)
            );
            return;
        }

        var commands = new List<(string, string)>();
        foreach (var tool in tools)
        {
            var code = GenerateCliCommand(tool);
            commands.Add((code.TypeName, tool.Name));
            spc.AddSource(code.OutputFileName, code);
        }

        var addCommands = GenerateCommandRegistration(commands);
        spc.AddSource(addCommands.OutputFileName, addCommands);

        var details = GenerateDetailedDescriptions(tools);
        spc.AddSource(details.OutputFileName, details);
    }

    /// <summary>
    /// The full text of the tools whose description runs to more than one line. Spectre reads one
    /// description for both the command list and a single command's help, and the list only has room
    /// for a line, so the help provider reads the detail from here instead.
    /// </summary>
    private CodeBlock GenerateDetailedDescriptions(ImmutableArray<ToolDetails> tools)
    {
        var detailed = tools
            .Where(it => it.Description.Contains('\n'))
            .Select(it => $"[\"{it.Name}\"] = \"{EscapeLiteral(it.Description)}\",")
            .ToArray();

        return new()
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

    private CodeBlock GenerateCommandRegistration(List<(string, string)> commands)
        => new()
        {
            Namespace = "Dev.Tools.Console.Commands",
            TypeName = "CommandConfigurator",
            GeneratorType = typeof(CliCommandGenerator),
            Placeholders = new()
            {
                ["AddCommand"] = string.Join("\n        ", commands.Select(it => $"ConfigureCommand<{it.Item1}>(services, configurator, \"{it.Item2}\");"))
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

    private CodeBlock GenerateCliCommand(ToolDetails tool) =>
        new()
        {
            Namespace = "Dev.Tools.Console.Commands",
            TypeName = tool.TypeName.Replace("Tool", "Command"),
            GeneratorType = typeof(CliCommandGenerator),
            Placeholders = new()
            {
                ["ToolName"] = tool.Name,
                ["ToolType"] = tool.TypeName,
                // Spectre shows this both as the DESCRIPTION block and as the row in the
                // root command list, so only the first line fits.
                ["ToolDescription"] = EscapeLiteral(FirstLine(tool.Description)),
                ["ToolArgsType"] = tool.ArgsDetails.Type,
                ["ToolResultType"] = tool.ResultDetails.Type,
                ["SettingsType"] = GenerateSettingsClass(tool),
                ["SettingsMapping"] = GenerateMapping(tool),
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
              $$"""
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
    
    private static string GenerateSettingsClass(ToolDetails tool)
    {
        var properties = new System.Text.StringBuilder();
        var map = new HashSet<string>();

        for(var i = 0; i < tool.ArgsDetails.Properties.Length; i++)
        {
            var prop = tool.ArgsDetails.Properties[i];
            var description = EscapeLiteral(prop.Description);
            
            // An option the user did not pass must keep the Args record's own default,
            // otherwise every unspecified option silently becomes default(T).
            // The property initializer is not enough on its own: Spectre binds absent
            // flags to false, and only honours the default through DefaultValueAttribute.
            var initializer = prop.DefaultValue ?? "default!";
            var defaultAttribute = prop.DefaultValue is null or "null"
                ? string.Empty
                : $"[System.ComponentModel.DefaultValue({prop.DefaultValue})]\n            ";

            var letter = prop.Name[0].ToString().ToLowerInvariant();
            var template = $"{letter + prop.Name.Substring(1)}";
            if (prop.IsPipeInput)
            {
                // NOTE: use optional argument template [argument] instead of <argument> to support StdIn
                properties.AppendLine($$"""
                                                [Description("{{description}}")]
                                                [CommandArgument(0, "[{{template}}]")]
                                                public {{prop.Type}} {{prop.Name}} { get; set; } = {{initializer}};

                                        """);
            }
            else
            {
                template = $"--{template}";
                if (map.Add(letter) || map.Add(letter = letter.ToUpperInvariant()))
                {
                    template = $"-{letter}|{template}";
                }

                properties.AppendLine($$"""
                                                [Description("{{description}}")]
                                                [CommandOption("{{template}}")]
                                                {{defaultAttribute}}public {{prop.Type}} {{prop.Name}} { get; set; } = {{initializer}};

                                        """);
            }
        }

        return $$"""
                 public sealed class Settings : SettingsBase
                     {
                 {{properties}}    }
                 """;
    }
    
    private static string GenerateMapping(ToolDetails tool)
    {
        // Check if Args type has a constructor matching all properties (positional record)
        var hasMatchingConstructor = HasConstructorMatchingProperties(tool.ArgsDetails.Symbol);

        if (hasMatchingConstructor)
        {
            // Use constructor syntax for positional records
            var arguments = string.Join(",\n                    ",
                tool.ArgsDetails.Properties.Select(p => $"{p.Name}: settings.{p.Name}"));

            return $$"""
                     var args = new {{tool.TypeName}}.Args(
                                    {{arguments}}
                                );
                     """;
        }
        else
        {
            // Use object initializer syntax for init properties
            var properties = string.Join(",\n                    ",
                tool.ArgsDetails.Properties.Select(p => $"{p.Name} = settings.{p.Name}"));

            return $$"""
                     var args = new {{tool.TypeName}}.Args
                                {
                                    {{properties}}
                                };
                     """;
        }
    }
}