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
                ["ToolTitle"] = EscapeLiteral(TitleOf(tool)),
                // A model reads the whole thing, usage note included.
                ["ToolDescription"] = EscapeLiteral(ModelDescription(tool)),
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
                "ModelContextProtocol",
                "ModelContextProtocol.Server"
            ],
            Content =
              $$"""
              [McpServerToolType]
              public static class {{tool.TypeName}}McpTool
              {
                  {ResultType}

                  // UseStructuredContent makes the SDK publish an output schema for the response
                  // type, which is the only way the result descriptions reach the model.
                  [McpServerTool(Name = "{ToolName}", Title = "{ToolTitle}", UseStructuredContent = true)]
                  [Description("{ToolDescription}")]
                  public static async Task<{ToolResultType}> ExecuteAsync(
                      IToolsProvider tools,
                      {ToolArgsParameters}
                      CancellationToken cancellationToken = default)
                  {
                      var tool = tools.GetTool<{ToolType}>();

                      {RequestTypeMapping}

                      var result = await tool.RunAsync(args, cancellationToken);

                      if (result.HasErrors)
                      {
                          throw new McpException("Tool '{ToolName}' failed: " + string.Join(", ", result.ErrorCodes));
                      }

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

        var paramsText = string.Join(",\n        ",
            argsDetails.Properties.Select(prop =>
            {
                var description = EscapeLiteral(prop.Description + EnumOptions(argsDetails, prop));
                var paramName = ToParamName(prop.Name);
                var paramDecl = $"[Description(\"{description}\")] {prop.Type} {paramName}";

                if (prop.DefaultValue != null)
                    paramDecl += $" = {prop.DefaultValue}";

                return paramDecl;
            }));

        return paramsText + ",";
    }

    /// <summary>
    /// What each value of an enum parameter means, folded into the parameter description.
    /// JSON Schema carries no per-value documentation - an enum reaches the model as a bare list of
    /// names - so this is the only way the authored value text gets in front of it.
    /// </summary>
    private static string EnumOptions(TypeDetails argsDetails, PropertyDetails prop)
    {
        var propertyType = prop.Type.TrimEnd('?');
        var enumDetails = argsDetails.Enums.FirstOrDefault(it => it.Type.TrimEnd('?') == propertyType);
        if (enumDetails is null || enumDetails.Values.Length == 0)
        {
            return string.Empty;
        }

        var described = enumDetails
            .Values
            .Where(it => !string.IsNullOrEmpty(it.Description))
            .Select(it => $"{it.Name} = {it.Description}")
            .ToArray();

        return described.Length == 0
            ? string.Empty
            : ". Options: " + string.Join("; ", described);
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
            var description = EscapeLiteral(prop.Description);

            properties.AppendLine($$"""
                                            [Description("{{description}}")]
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
