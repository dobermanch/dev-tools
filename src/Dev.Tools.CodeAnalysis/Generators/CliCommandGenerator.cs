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
        
        var compilationAndAttributes = referencedToolTypes
            .Combine(attributes)
            .Where(it => it.Right != null)
            .Select((it, _) => it.Left)
            .Collect();
        
        context.RegisterSourceOutput(compilationAndAttributes, GenerateTools);
    }

    protected override void GenerateTools(SourceProductionContext spc, ImmutableArray<ToolDetails> tools)
    {
        foreach (var tool in tools)
        {
            var code = GenerateCliCommand(tool);
            spc.AddSource(code.OutputFileName, code);
        }
    }

    private CodeBlock GenerateCliCommand(ToolDetails tool) =>
        new()
        {
            Namespace = "Dev.Tools.Console.Commands",
            TypeName = tool.TypeName + "Command",
            GeneratorType = typeof(CliCommandGenerator),
            Placeholders = new()
            {
                ["ToolName"] = tool.Name,
                ["ToolType"] = tool.TypeName,
                ["ToolArgsType"] = tool.ArgsDetails.Type,
                ["ToolResultType"] = tool.ResultDetails.Type
            },
            Usings = [
                "Dev.Tools.Core",
                "Dev.Tools.Console.Core",
                "Dev.Tools.Core.Localization",
                "Dev.Tools.Providers",
                "Dev.Tools.Tools",
                "Spectre.Console",
                "Spectre.Console.Cli",
            ],
            Content = 
              """
              internal sealed partial class {TypeName}(Dev.Tools.Providers.IToolsProvider toolProvider, IToolResponseHandler responseHandler) 
                  : AsyncCommand<{TypeName}.Settings>
              {
                  public sealed class Settings : CommandSettings
                  {
                  }
                  
                  public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
                  {
                      ToolDefinition definition = toolProvider.GetToolDefinition<{ToolType}>();
                      
                      try
                      {
                          var tool = toolProvider.GetTool<{ToolType}>();
                          var args = new {ToolType}.Args
                          {
                          };
                      
                          var result = await tool.RunAsync(args, CancellationToken.None);
                          
                          return responseHandler.ProcessResponse(result, definition);
                      }
                      catch (Exception ex)
                      {
                          return responseHandler.ProcessError(ex, definition);
                      }
                  }
              }
              """
        };
}