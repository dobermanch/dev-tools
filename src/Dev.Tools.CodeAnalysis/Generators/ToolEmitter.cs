using System.Text;
using Dev.Tools.CodeAnalysis.Core;

namespace Dev.Tools.CodeAnalysis.Generators;

/// <summary>
/// The code fragments more than one frontend needs: the DTO mirroring a tool's arguments or result,
/// and the statement that maps one shape onto another. The API and MCP generators held
/// character-identical copies of both, and the CLI a third variant of the mapping.
/// </summary>
public static class ToolEmitter
{
    /// <summary>
    /// A flat, settable class mirroring the tool's <c>Args</c> or <c>Result</c>, carrying each
    /// property's authored description so it reaches the OpenAPI and MCP schemas.
    /// </summary>
    public static string DataType(ToolModel tool, TypeModel type, string suffix)
    {
        var properties = new StringBuilder();
        var toolName = ToolNaming.BaseName(tool.TypeName);

        foreach (var property in type.Properties)
        {
            var description = ToolText.EscapeLiteral(property.Description);

            properties.AppendLine($$"""
                                            [Description("{{description}}")]
                                            public {{property.Type}} {{property.Name}} { get; set; } = default!;

                                    """);
        }

        return $$"""
                 public sealed class {{toolName}}{{suffix}}
                     {
                 {{properties}}    }
                 """;
    }

    /// <summary>
    /// <c>var {target} = new {typeName}(...)</c>, using constructor syntax for a positional record
    /// and an object initializer otherwise.
    /// </summary>
    /// <param name="valueOf">The expression each property is assigned from.</param>
    /// <param name="forceObjectInitializer">
    /// Set for a type the generator declares itself: those are always plain classes with settable
    /// properties, whatever the tool's own record looks like.
    /// </param>
    public static string Mapping(
        TypeModel type,
        string typeName,
        string target,
        Func<PropertyModel, string> valueOf,
        bool forceObjectInitializer = false)
    {
        if (!forceObjectInitializer && type.HasPositionalConstructor)
        {
            var arguments = string.Join(",\n                    ",
                type.Properties.Select(it => $"{it.Name}: {valueOf(it)}"));

            return $$"""
                     var {{target}} = new {{typeName}}(
                                    {{arguments}}
                                );
                     """;
        }

        var assignments = string.Join(",\n                    ",
            type.Properties.Select(it => $"{it.Name} = {valueOf(it)}"));

        return $$"""
                 var {{target}} = new {{typeName}}
                            {
                                {{assignments}}
                            };
                 """;
    }
}
