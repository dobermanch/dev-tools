using Microsoft.CodeAnalysis;

namespace Dev.Tools.CodeAnalysis.Generators;

//[Generator]
public class CommandGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var tools = context
            .CompilationProvider
            .SelectMany((compilation, _) =>
                compilation
                    .References
                    .Where(it =>
                        it.Display is not null
                        && it.Display.Contains("Dev.Tools"))
                    .Select(it => compilation.GetAssemblyOrModuleSymbol(it))
                    .OfType<IAssemblySymbol>()
                    .SelectMany(it => GetAllTypes(it.GlobalNamespace))
                   // .Where(it => it.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == typeof(ToolDefinitionAttribute).FullName))
                    .Select(it => {
                     //   var attr = it.GetAttributes().First(attr => attr.AttributeClass?.ToDisplayString() == typeof(ToolDefinitionAttribute).FullName);
                        var types = it.GetMembers().OfType<ITypeSymbol>().ToArray();

                        return new ToolDefinition
                        {
                            FullName = it.ToDisplayString(),
                            ClassName = it.Name,
                            //Name = (string?)attr.NamedArguments.FirstOrDefault(it => it.Key == "Name").Value.Value,
                            //Alieases = attr.NamedArguments.FirstOrDefault(it => it.Key == "Aliases").Value.Values.Select(it => it.Value).OfType<string>().ToArray(),
                            // Args = types.Where(it => it.BaseType?.ToDisplayString() == typeof(ToolArgs).FullName)
                            //             .Select(GetTypeDetails)
                            //             .FirstOrDefault(),
                            // Result = types.Where(it => it.BaseType?.ToDisplayString() == typeof(ToolResult).FullName)
                            //             .Select(GetTypeDetails)
                            //             .FirstOrDefault(),
                            Location = it.Locations.FirstOrDefault()
                        };
                    })
             );


        context.RegisterSourceOutput(tools, (ctx, toolDefinition) =>
        {
            var source = GenerateCommandClass(toolDefinition);
            ctx.AddSource($"{toolDefinition.ClassName}Command.g.cs", source);
        });
    }

    private static string GenerateCommandClass(ToolDefinition toolDef)
    {
        var className = $"{toolDef.ClassName}Command";
        var settingsClassName = $"{className}.Settings";
        var argsClassName = $"{toolDef.ClassName}.Args";
        var resultClassName = $"{toolDef.ClassName}.Result";

        return $@"//--------------------
// auto-generated
//--------------------
using Dev.Tools.Core.Localization;
using Dev.Tools.Tools;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;

#nullable enable

namespace Dev.Tools.Console.Commands;

internal sealed partial class {className} : AsyncCommand<{settingsClassName}>
{{
    private readonly {toolDef.FullName} _tool;
    private readonly IToolResponseHandler _responseHandler;

    public {className}({toolDef.FullName} tool, IToolResponseHandler responseHandler)
    {{
        _tool = tool;
        _responseHandler = responseHandler;
    }}

    public sealed class Settings : CommandSettings
    {{
{string.Join("\n", toolDef.Args?
    .Properties
    .Select(p => $@"
        [LocalizedDescription(""{p.Name}"")]
        [CommandArgument(0, ""[{p.Name.ToLower()}]"")]
        public {p.Type} {p.Name} {{ get; init; }} = default!;")
)}
    }}

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {{
        var args = new {argsClassName}(
            {string.Join(", ", toolDef.Args?.Properties.Select(p => $"settings.{p.Name}") ?? new string[] { })}
        );
        
        PostUpdateArgs(ref args, context, settings);

        {resultClassName} result = await _tool.RunAsync(args, CancellationToken.None);

        return _responseHandler.Handle(result);
    }}

    partial void PostUpdateArgs(ref {argsClassName} args, CommandContext context, Settings settings);
}}";
    }

    private record struct ToolDefinition
    (
        string? FullName,
        string? ClassName,
        string? Description,
        string? Name,
        string[]? Alieases,
        TypeDetails? Args,
        TypeDetails? Result,
        Location? Location
    );

    private record struct TypeDetails
    (
        string Name,
        (string Name, string Type)[] Properties
    );


    private static IEnumerable<ITypeSymbol> GetAllTypes(INamespaceSymbol root)
    {
        foreach (var namespaceOrTypeSymbol in root.GetMembers())
        {
            if (namespaceOrTypeSymbol is INamespaceSymbol @namespace)
            {
                foreach (var nested in GetAllTypes(@namespace))
                {
                    yield return nested;
                }
            }
            else if (namespaceOrTypeSymbol is ITypeSymbol type)
            {
                yield return type;
            }
        }
    }

    private static TypeDetails? GetTypeDetails(ITypeSymbol typeSymbol)
    {
        var properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where( p => p.Name != "EqualityContract")
            .Select(p => (p.Name, p.Type.ToDisplayString()))
            .ToArray();

        return new TypeDetails(typeSymbol.Name, properties);
    }
}