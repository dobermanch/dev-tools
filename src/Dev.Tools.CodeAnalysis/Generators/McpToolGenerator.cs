using System.Collections.Immutable;
using Dev.Tools.CodeAnalysis.Core;
using Dev.Tools.Mcp.Core;
using Microsoft.CodeAnalysis;

namespace Dev.Tools.CodeAnalysis.Generators;

[Generator]
public class McpToolGenerator : ToolGeneratorBase, IIncrementalGenerator
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

        var attributes = GetAssemblyAttribute<GenerateToolsMcpToolAttribute>(context.CompilationProvider);

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
            var code = GenerateTool(tool);
            spc.AddSource(code.OutputFileName, code);
        }
    }

    private CodeBlock GenerateTool(ToolDetails tool)
    {
        return new()
        {
            Namespace = "Dev.Tools.Mcp.Tools",
            TypeName = tool.TypeName.Replace("Tool", "McpTool"),
            GeneratorType = typeof(McpToolGenerator),
            Placeholders = new()
            {
                ["ToolName"] = tool.Name,
                ["ToolType"] = tool.TypeName,
                ["ToolLocalizationName"] = tool.TypeName.Replace("Tool", ""),
                ["ToolResultType"] = tool.TypeName.Replace("Tool", "Response"),
                ["ToolArgsParameters"] = GenerateIndividualParameters(tool, tool.ArgsDetails),
                ["RequestTypeMapping"] = GenerateArgsFromIndividualParameters(tool.ArgsDetails),
                ["ResultType"] = GenerateDataType(tool, tool.ResultDetails, "Response"),
                ["ResultTypeMapping"] = GenerateDataTypeMapping(tool.ResultDetails, tool.TypeName.Replace("Tool", "Response"), "response", "result", true)
            },
            Usings = [
                "System.ComponentModel",
                "Dev.Tools.Providers",
                "Dev.Tools.Tools",
                "Dev.Tools.Localization",
                "ModelContextProtocol.Server"
            ],
            Content =
              $$"""
              [McpServerToolType]
              public static class {{tool.TypeName}}McpTool
              {
                  {ResultType}

                  [McpServerTool(Name = "{ToolName}")]
                  [LocalizedDescription("Tools.{ToolLocalizationName}.Description")]
                  public static async Task<{ToolResultType}> ExecuteAsync(
                      IToolsProvider tools,
                      {ToolArgsParameters}
                      CancellationToken cancellationToken = default)
                  {
                      var tool = tools.GetTool<{ToolType}>();

                      {RequestTypeMapping}

                      var result = await tool.RunAsync(args, cancellationToken);

                      {ResultTypeMapping}

                      return response;
                  }
              }
              """
        };
    }

    private static readonly HashSet<string> s_csharpKeywords = new HashSet<string>
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while"
    };

    private static string ToParamName(string propertyName)
    {
        var name = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
        return s_csharpKeywords.Contains(name) ? "@" + name : name;
    }

    private static string GenerateIndividualParameters(ToolDetails tool, TypeDetails argsDetails)
    {
        if (argsDetails.Properties.Length == 0) return string.Empty;

        var toolName = tool.TypeName.Replace("Tool", "");

        var paramsText = string.Join(",\n        ",
            argsDetails.Properties.Select(prop =>
            {
                var description = $"Tools.{toolName}.{argsDetails.TypeName}.{prop.Name}.Description";
                var paramName = ToParamName(prop.Name);
                var paramDecl = $"[LocalizedDescription(\"{description}\")] {prop.Type} {paramName}";

                if (prop.DefaultValue != null)
                    paramDecl += $" = {prop.DefaultValue}";

                return paramDecl;
            }));

        return paramsText + ",";
    }

    private static string GenerateArgsFromIndividualParameters(TypeDetails argsDetails)
    {
        if (HasConstructorMatchingProperties(argsDetails.Symbol))
        {
            var arguments = string.Join(",\n                    ",
                argsDetails.Properties.Select(p =>
                {
                    var paramName = ToParamName(p.Name);
                    return $"{p.Name}: {paramName}";
                }));

            return $$"""
                     var args = new {{argsDetails.Type}}(
                                    {{arguments}}
                                );
                     """;
        }
        else
        {
            var properties = string.Join(",\n                    ",
                argsDetails.Properties.Select(p =>
                {
                    var paramName = ToParamName(p.Name);
                    return $"{p.Name} = {paramName}";
                }));

            return $$"""
                     var args = new {{argsDetails.Type}}
                                {
                                    {{properties}}
                                };
                     """;
        }
    }

    private static string GenerateDataType(ToolDetails tool, TypeDetails typeDetails, string toolNameSuffix)
    {
        var properties = new System.Text.StringBuilder();
        var toolName = tool.TypeName.Replace("Tool", "");

        foreach (var prop in typeDetails.Properties)
        {
            var description = $"Tools.{toolName}.{typeDetails.TypeName}.{prop.Name}.Description";

            properties.AppendLine($$"""
                                            [LocalizedDescription("{{description}}")]
                                            public {{prop.Type}} {{prop.Name}} { get; set; } = default!;

                                    """);
        }

        return $$"""
                 public sealed class {{toolName}}{{toolNameSuffix}}
                     {
                 {{properties}}    }
                 """;
    }

    private static string GenerateDataTypeMapping(TypeDetails typeDetails, string typeName, string targetVariableName, string sourceVariableName, bool useParameterInitializer)
    {
        // Check if Result type has a constructor matching all properties (positional record)
        if (!useParameterInitializer && HasConstructorMatchingProperties(typeDetails.Symbol))
        {
            var arguments = string.Join(",\n                    ",
                typeDetails.Properties.Select(p => $"{p.Name}: {sourceVariableName}.{p.Name}"));

            return $$"""
                     var {{targetVariableName}} = new {{typeName}}(
                                    {{arguments}}
                                );
                     """;
        }
        else
        {
            var properties = string.Join(",\n                    ",
                typeDetails.Properties.Select(p => $"{p.Name} = {sourceVariableName}.{p.Name}"));

            return $$"""
                     var {{targetVariableName}} = new {{typeName}}
                                {
                                    {{properties}}
                                };
                     """;
        }
    }
}
