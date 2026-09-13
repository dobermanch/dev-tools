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
    /// <param name="typeOf">
    /// The declared type of a property, when a frontend binds it as something other than
    /// <see cref="PropertyModel.BindingType"/> - an upload, say.
    /// </param>
    public static string DataType(
        ToolModel tool,
        TypeModel type,
        string suffix,
        Func<PropertyModel, string>? typeOf = null)
    {
        var properties = new StringBuilder();
        var toolName = ToolNaming.BaseName(tool.TypeName);

        foreach (var property in type.Properties)
        {
            var description = ToolText.EscapeLiteral(property.Description);
            var propertyType = typeOf?.Invoke(property) ?? property.BindingType;

            // A value the caller leaves out has to keep the tool's own default, as it does on the CLI and
            // in MCP; with default! an omitted argument arrived as default(T) instead. Only where the
            // declared type is the tool's own - a default does not carry over to an upload.
            var initializer = propertyType == property.Type && property.DefaultValue is not null
                ? property.DefaultValue
                : "default!";

            properties.AppendLine($$"""
                                            [Description("{{description}}")]
                                            public {{propertyType}} {{property.Name}} { get; set; } = {{initializer}};

                                    """);
        }

        return $$"""
                 public sealed class {{toolName}}{{suffix}}
                     {
                 {{properties}}    }
                 """;
    }

    /// <summary>
    /// The expression that hands a frontend's value to the tool. A <c>ToolContent</c> argument arrives
    /// as text and is wrapped here; anything else passes through unchanged.
    /// </summary>
    public static string ArgumentValue(PropertyModel property, string expression)
        => property.IsContent ? $"global::Dev.Tools.ToolContent.FromText({expression})" : expression;

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
