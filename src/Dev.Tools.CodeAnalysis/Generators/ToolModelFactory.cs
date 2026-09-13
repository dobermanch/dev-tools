using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.CodeAnalysis.Generators;

/// <summary>
/// Reads a <c>[ToolDefinition]</c> class into a <see cref="ToolModel"/>. This is the only place in
/// the generator layer that touches Roslyn symbols; everything downstream works on plain data.
/// </summary>
public static class ToolModelFactory
{
    private const string DescriptionAttribute = "System.ComponentModel.DescriptionAttribute";
    private const string DisplayNameAttribute = "System.ComponentModel.DisplayNameAttribute";

    public static ToolExtraction Create(INamedTypeSymbol symbol)
    {
        var attribute = ReadToolAttribute(symbol);
        if (attribute is null)
        {
            return ToolExtraction.Skipped(symbol.Name, "it carries no [ToolDefinition] attribute");
        }

        if (string.IsNullOrEmpty(attribute.Name))
        {
            return ToolExtraction.Skipped(symbol.Name, "its [ToolDefinition] sets no Name");
        }

        var toolInterface = FindToolInterface(symbol);
        if (toolInterface is null)
        {
            return ToolExtraction.Skipped(
                symbol.Name,
                $"it does not implement {CodeDefinitions.ToolsGlobalNamespace}.ITool<TArgs, TResult>");
        }

        var args = ReadType(toolInterface.TypeArguments[0]);
        var result = ReadType(toolInterface.TypeArguments[1]);

        // Error codes are read from the method bodies, which only exist when the tool is being
        // compiled from source. A frontend generator reads the tool from metadata and gets none -
        // it has no use for them either, the catalog is their only consumer.
        var errorCodes = symbol.DeclaringSyntaxReferences.Length > 0
            ? FindThrownErrorCodes(symbol)
            : [];

        return ToolExtraction.Extracted(new ToolModel(
            Name: attribute.Name!,
            TypeName: symbol.Name,
            Namespace: symbol.ContainingNamespace.ToDisplayString(),
            Title: attribute.Title,
            Description: attribute.Description,
            Usage: attribute.Usage,
            Aliases: attribute.Aliases,
            Categories: attribute.Categories,
            Keywords: attribute.Keywords,
            ErrorCodes: new[] { "ErrorCode.Unknown" }.Concat(errorCodes).ToEquatableArray(),
            Args: args,
            Result: result));
    }

    /// <summary>
    /// The tool's <c>ITool&lt;TArgs, TResult&gt;</c>, matched by name rather than by shape. This used
    /// to accept any generic interface with two type arguments, so a tool that happened to implement
    /// something like <c>IComparer&lt;,&gt;</c> first would have had that interface's type arguments
    /// generated as its arguments and result.
    /// </summary>
    private static INamedTypeSymbol? FindToolInterface(INamedTypeSymbol symbol)
        => symbol.AllInterfaces.FirstOrDefault(it =>
            it.MetadataName == CodeDefinitions.ToolInterfaceMetadataName
            && it.ContainingNamespace.ToDisplayString() == CodeDefinitions.ToolsGlobalNamespace);

    private static TypeModel ReadType(ITypeSymbol typeSymbol)
    {
        var type = typeSymbol.ToDisplayString();
        var properties = PublicProperties(typeSymbol);
        var defaults = ConstructorDefaults(typeSymbol);

        return new TypeModel(
            Type: type,
            SimpleName: type.Split('.').Last(),
            HasPositionalConstructor: HasConstructorMatchingProperties(typeSymbol, properties),
            Properties: properties
                .Select(it => new PropertyModel(
                    Name: it.Name,
                    Type: it.Type.ToDisplayString(),
                    IsRequired: it.IsRequired,
                    IsNullable: it.NullableAnnotation == NullableAnnotation.Annotated,
                    IsPipeInput: it.GetAttributes().Any(a => a.AttributeClass?.Name == "PipeInputAttribute"),
                    IsPipeOutput: it.GetAttributes().Any(a => a.AttributeClass?.Name == "PipeOutputAttribute"),
                    Description: AttributeText(it, DescriptionAttribute),
                    DisplayName: AttributeText(it, DisplayNameAttribute),
                    DefaultValue: defaults.TryGetValue(it.Name, out var value) ? value : null))
                .ToEquatableArray(),
            Enums: ReadEnums(properties));
    }

    private static IPropertySymbol[] PublicProperties(ITypeSymbol typeSymbol)
        => typeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(it => it.DeclaredAccessibility == Accessibility.Public)
            .ToArray();

    private static EquatableArray<EnumModel> ReadEnums(IPropertySymbol[] properties)
        => properties
            .Select(it => it.Type)
            .OfType<INamedTypeSymbol>()
            .Where(it => it.TypeKind == TypeKind.Enum && !IsCoreEnum(it))
            .GroupBy(it => it.ToDisplayString())
            .Select(group =>
            {
                var type = group.Key;
                return new EnumModel(
                    Type: type,
                    SimpleName: type.Split('.').Last(),
                    Values: group.First()
                        .GetMembers()
                        .OfType<IFieldSymbol>()
                        .Where(it => it.HasConstantValue)
                        .Select(it => new EnumValueModel(it.Name, AttributeText(it, DescriptionAttribute)))
                        .ToEquatableArray());
            })
            .ToEquatableArray();

    /// <summary>
    /// Category, Keyword and ErrorCode are the library's own enums. Every tool would otherwise
    /// re-describe them, and the frontends already carry their text once.
    /// </summary>
    private static bool IsCoreEnum(INamedTypeSymbol enumType)
        => enumType.ContainingNamespace.ToDisplayString() == CodeDefinitions.ToolsGlobalNamespace
           && enumType.Name is "Category" or "Keyword" or "ErrorCode";

    private static bool HasConstructorMatchingProperties(ITypeSymbol typeSymbol, IPropertySymbol[] properties)
        => typeSymbol is INamedTypeSymbol namedType
           && namedType.Constructors
               .Where(it => it.DeclaredAccessibility == Accessibility.Public && !it.IsImplicitlyDeclared)
               .Any(ctor => ctor.Parameters.Length == properties.Length
                            && ctor.Parameters.All(parameter => properties.Any(property =>
                                string.Equals(property.Name, parameter.Name, StringComparison.OrdinalIgnoreCase))));

    private static Dictionary<string, string> ConstructorDefaults(ITypeSymbol typeSymbol)
    {
        var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (typeSymbol is not INamedTypeSymbol namedType)
        {
            return defaults;
        }

        // Which constructor is "the" constructor only matters when there are several, and DT1012
        // stops a tool from getting there.
        var constructor = namedType.Constructors
            .FirstOrDefault(it => it.DeclaredAccessibility == Accessibility.Public && !it.IsImplicitlyDeclared);
        if (constructor is null)
        {
            return defaults;
        }

        foreach (var parameter in constructor.Parameters.Where(it => it.HasExplicitDefaultValue))
        {
            defaults[parameter.Name] = FormatDefault(parameter);
        }

        return defaults;
    }

    private static string FormatDefault(IParameterSymbol parameter)
    {
        var value = parameter.ExplicitDefaultValue;

        // Unwrap Nullable<T> so a nullable enum default still renders as a member name.
        var type = parameter.Type;
        if (type is INamedTypeSymbol { IsGenericType: true } nullable
            && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            type = nullable.TypeArguments[0];
        }

        if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType && value is not null)
        {
            var member = enumType.GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(it => it.HasConstantValue && Equals(it.ConstantValue, value));

            return member is not null
                ? $"{enumType.ToDisplayString()}.{member.Name}"
                : value.ToString();
        }

        return value switch
        {
            null => "null",
            bool flag => flag ? "true" : "false",
            char character => $"'{character}'",
            string text => $"\"{text}\"",
            _ => value.ToString()
        };
    }

    private static string AttributeText(ISymbol symbol, string attributeFullName)
        => ToolText.Unwrap(symbol.GetAttributes()
            .FirstOrDefault(it => it.AttributeClass?.ToDisplayString() == attributeFullName)
            ?.ConstructorArguments.FirstOrDefault().Value?.ToString());

    private static string[] FindThrownErrorCodes(INamedTypeSymbol symbol)
        => symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .SelectMany(it => it.DeclaringSyntaxReferences)
            .Select(it => it.GetSyntax())
            .OfType<MethodDeclarationSyntax>()
            .SelectMany(it => it
                .DescendantNodes()
                .OfType<ThrowStatementSyntax>()
                .Select(x => x.Expression)
                .OfType<ObjectCreationExpressionSyntax>()
                .Where(x => x.Type.ToFullString() == "ToolException")
                .Select(x => x.ArgumentList!.Arguments[0].Expression.ToString()))
            .ToArray();

    private static ToolAttributeValues? ReadToolAttribute(INamedTypeSymbol symbol)
    {
        var attribute = symbol.GetAttributes()
            .FirstOrDefault(it => it.AttributeClass?.ToDisplayString()
                                  == CodeDefinitions.ToolDefinitionAttribute.TypeFullName);
        if (attribute is null)
        {
            return null;
        }

        var values = new ToolAttributeValues();
        foreach (var argument in attribute.NamedArguments)
        {
            switch (argument.Key)
            {
                case "Name":
                    values.Name = argument.Value.Value?.ToString();
                    break;
                case "Aliases":
                    values.Aliases = StringArray(argument.Value);
                    break;
                case "Categories":
                    values.Categories = EnumArray(argument.Value);
                    break;
                case "Keywords":
                    values.Keywords = EnumArray(argument.Value);
                    break;
                case "Title":
                    values.Title = ToolText.Unwrap(argument.Value.Value?.ToString());
                    break;
                case "Description":
                    values.Description = ToolText.Unwrap(argument.Value.Value?.ToString());
                    break;
                case "Usage":
                    values.Usage = ToolText.Unwrap(argument.Value.Value?.ToString());
                    break;
            }
        }

        return values;
    }

    /// <summary>String values, quoted ready for emission.</summary>
    private static string[] StringArray(TypedConstant constant)
        => constant.Kind != TypedConstantKind.Array
            ? []
            : constant.Values.Select(it => $"\"{it.Value?.ToString() ?? string.Empty}\"").ToArray();

    /// <summary>Enum values, rendered as "Enum.Member" ready for emission.</summary>
    private static string[] EnumArray(TypedConstant constant)
        => constant.Kind != TypedConstantKind.Array
            ? []
            : constant.Values
                .Where(it => it.Type is INamedTypeSymbol { TypeKind: TypeKind.Enum })
                .Select(it =>
                {
                    var enumType = (INamedTypeSymbol)it.Type!;
                    var member = enumType.GetMembers()
                        .OfType<IFieldSymbol>()
                        .FirstOrDefault(field => field.HasConstantValue && Equals(field.ConstantValue, it.Value));

                    return member is not null
                        ? $"{enumType.Name}.{member.Name}"
                        : it.Value?.ToString() ?? string.Empty;
                })
                .ToArray();

    private sealed class ToolAttributeValues
    {
        public string? Name { get; set; }
        public string[] Aliases { get; set; } = [];
        public string[] Categories { get; set; } = [];
        public string[] Keywords { get; set; } = [];
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Usage { get; set; } = string.Empty;
    }
}
