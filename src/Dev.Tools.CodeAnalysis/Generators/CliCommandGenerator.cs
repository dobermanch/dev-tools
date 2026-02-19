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
        var compilationWithMetadata =
            context.CompilationProvider
                .Select((compilation, _) =>
                {
                    var assemblies = compilation.References
                        .Select(compilation.GetAssemblyOrModuleSymbol)
                        .OfType<IAssemblySymbol>()
                        .ToList();

                    var toolsAssembly = assemblies.FirstOrDefault(it => it.Name == CodeDefinitions.ToolsAssemblyName);

                    var assemblyNames = string.Join(", ", assemblies.Select(a => a.Name));

                    List<ITypeSymbol> allTypes = [];
                    List<ITypeSymbol> typesWithAttribute = [];

                    if (toolsAssembly != null)
                    {
                        allTypes = toolsAssembly.GlobalNamespace.GetAllTypes().ToList();
                        typesWithAttribute = allTypes
                            .Where(t => t.HasAttribute(CodeDefinitions.ToolDefinitionAttribute.TypeFullName))
                            .ToList();
                    }

                    return (
                        compilation,
                        toolsAssembly,
                        referenceCount: assemblies.Count,
                        assemblyNames,
                        typeCount: allTypes.Count,
                        attributedTypeCount: typesWithAttribute.Count,
                        tools: typesWithAttribute
                            .Select(t => GetToolDetails((INamedTypeSymbol)t))
                            .Where(t => t != null)
                            .Select(t => t!)
                            .ToList()
                    );
                });

        var attributes = GetAssemblyAttribute<GenerateToolsCliCommandAttribute>(context.CompilationProvider);

        var compilationAndTools = compilationWithMetadata
            .Combine(attributes)
            .Select((pair, _) =>
            {
                var (compilation, toolsAssembly, refCount, asmNames, typeCount, attrTypeCount, tools) = pair.Left;
                var attribute = pair.Right;

                return (compilation, toolsAssembly, refCount, asmNames, typeCount, attrTypeCount, tools, attribute);
            });

        context.RegisterSourceOutput(compilationAndTools, (spc, data) =>
        {
            var (compilation, toolsAssembly, refCount, asmNames, typeCount, attrTypeCount, tools, attribute) = data;

            if (attribute == null)
            {
                return; // Don't generate if attribute not present
            }

            GenerateTools(spc, tools.ToImmutableArray());
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
                ["ToolDescriptionLocalizationKey"] = $"Tools.{tool.TypeName.Replace("Tool", "")}.Description",
                ["ToolArgsType"] = tool.ArgsDetails.Type,
                ["ToolResultType"] = tool.ResultDetails.Type,
                ["SettingsType"] = GenerateSettingsClass(tool),
                ["SettingsMapping"] = GenerateMapping(tool),
            },
            Usings = [
                "Dev.Tools.Console.Core",
                "Dev.Tools.Localization",
                "Dev.Tools.Providers",
                "Dev.Tools.Tools",
                "Spectre.Console",
                "Spectre.Console.Cli",
            ],
            Content = 
              $$"""
                [LocalizedDescription("{ToolDescriptionLocalizationKey}")]
                internal sealed partial class {TypeName}(Dev.Tools.Providers.IToolsProvider toolProvider, IToolResponseHandler responseHandler) 
                    : AsyncCommand<{TypeName}.Settings>
                {
                    {SettingsType}
                    
                    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
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
            var toolName = tool.TypeName.Replace("Tool", "");
            var description = $"Tools.{toolName}.{tool.ArgsDetails.TypeName}.{prop.Name}.Description";
            
            var letter = prop.Name[0].ToString().ToLower();
            var template = $"{letter + prop.Name.Substring(1)}";
            if (prop.IsPipeInput)
            {
                properties.AppendLine($$"""
                                                [LocalizedDescription("{{description}}")]
                                                [CommandArgument(0, "<{{template}}>")]
                                                public {{prop.Type}} {{prop.Name}} { get; set; } = default!;

                                        """);
            }
            else
            {
                template = $"--{template}";
                if (map.Add(letter) || map.Add(letter = letter.ToUpper()))
                {
                    template = $"-{letter}|{template}";
                }

                properties.AppendLine($$"""
                                                [LocalizedDescription("{{description}}")]
                                                [CommandOption("{{template}}")]
                                                public {{prop.Type}} {{prop.Name}} { get; set; } = default!;

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