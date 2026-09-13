using System.Collections.Immutable;
using Dev.Tools.CodeAnalysis.Core;
using Dev.Tools.Mcp.Core;
using Microsoft.CodeAnalysis;

namespace Dev.Tools.CodeAnalysis.Generators;

[Generator]
public class McpToolGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
        => ToolPipeline.RegisterFrontend<GenerateToolsMcpToolAttribute>(context, "MCP tool", Generate);

    private static void Generate(SourceProductionContext spc, ImmutableArray<ToolModel> tools)
    {
        foreach (var tool in tools)
        {
            var code = GenerateTool(tool);
            spc.AddSource(code.OutputFileName, code);
        }
    }

    private static CodeBlock GenerateTool(ToolModel tool)
    {
        var responseType = ToolNaming.ResponseType(tool.TypeName);

        return new CodeBlock
        {
            Namespace = "Dev.Tools.Mcp.Tools",
            // The holder class and the file it lands in have to agree: the file was named from the
            // trimmed base name while the class was spelled out in the template, so
            // HashTextMcpTool.g.cs declared a class called HashTextToolMcpTool.
            TypeName = ToolNaming.McpToolType(tool.TypeName),
            GeneratorType = typeof(McpToolGenerator),
            Placeholders = new()
            {
                ["ToolName"] = tool.Name,
                ["ToolType"] = tool.TypeName,
                ["ToolTitle"] = ToolText.EscapeLiteral(tool.DisplayTitle),
                // A model reads the whole thing, usage note included.
                ["ToolDescription"] = ToolText.EscapeLiteral(tool.ModelDescription),
                ["ToolResultType"] = responseType,
                ["ToolArgsParameters"] = GenerateIndividualParameters(tool.Args),
                ["RequestTypeMapping"] = ToolEmitter.Mapping(
                    tool.Args, tool.Args.Type, "args", it => ToParamName(it.Name)),
                ["ResultType"] = ToolEmitter.DataType(tool, tool.Result, "Response"),
                ["ResultTypeMapping"] = ToolEmitter.Mapping(
                    tool.Result, responseType, "response", it => $"result.{it.Name}",
                    forceObjectInitializer: true)
            },
            Usings = [
                "System.ComponentModel",
                "Dev.Tools.Providers",
                "Dev.Tools.Tools",
                "ModelContextProtocol",
                "ModelContextProtocol.Server",
                "Dev.Tools.Mcp.Core"
            ],
            Content =
              """
              [McpServerToolType]
              public static class {TypeName}
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
                          throw new McpException(ToolErrorMessage.For("{ToolName}", result.Errors));
                      }

                      {ResultTypeMapping}

                      return response;
                  }
              }
              """
        };
    }

    private static readonly HashSet<string> s_csharpKeywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while"
    ];

    private static string ToParamName(string propertyName)
    {
        var name = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
        return s_csharpKeywords.Contains(name) ? "@" + name : name;
    }

    /// <summary>
    /// The tool's arguments as individual method parameters: the MCP SDK builds the input schema
    /// from the method signature, so each one carries its own description and default.
    /// </summary>
    private static string GenerateIndividualParameters(TypeModel args)
    {
        if (args.Properties.Length == 0)
        {
            return string.Empty;
        }

        var parameters = string.Join(",\n        ",
            args.Properties.Select(property =>
            {
                var description = ToolText.EscapeLiteral(property.Description + EnumOptions(args, property));
                var declaration = $"[Description(\"{description}\")] {property.Type} {ToParamName(property.Name)}";

                return property.DefaultValue is null
                    ? declaration
                    : declaration + $" = {property.DefaultValue}";
            }));

        return parameters + ",";
    }

    /// <summary>
    /// What each value of an enum parameter means, folded into the parameter description.
    /// JSON Schema carries no per-value documentation - an enum reaches the model as a bare list of
    /// names - so this is the only way the authored value text gets in front of it.
    /// </summary>
    private static string EnumOptions(TypeModel args, PropertyModel property)
    {
        var enumModel = args.Enums.FirstOrDefault(it => it.Type.TrimEnd('?') == property.NonNullableType);
        if (enumModel is null)
        {
            return string.Empty;
        }

        var described = enumModel
            .Values
            .Where(it => !string.IsNullOrEmpty(it.Description))
            .Select(it => $"{it.Name} = {it.Description}")
            .ToArray();

        return described.Length == 0
            ? string.Empty
            : ". Options: " + string.Join("; ", described);
    }
}
