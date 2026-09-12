using System.Collections.Immutable;
using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.CodeAnalysis.Generators;

public abstract class ToolGeneratorBase
{
    protected abstract void GenerateTools(SourceProductionContext spc, ImmutableArray<ToolDetails> tools);
    
    protected IncrementalValueProvider<AttributeData?> GetAssemblyAttribute<T>(IncrementalValueProvider<Compilation> provider)
    {
        var guardAttribute = typeof(T);
        return provider.Select((compilation, _) =>
        {
            var attrSymbol = compilation.GetTypeByMetadataName($"{guardAttribute.Namespace}.{guardAttribute.Name}");
            return compilation.Assembly.GetAttributes().FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attrSymbol));
        });
    }
    
    protected static ToolDetails? GetToolDetails(INamedTypeSymbol symbol)
    {
        // Try to get syntax (available for source, not for metadata references)
        var syntax = symbol.DeclaringSyntaxReferences
            .Select(it => it.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();

        // Get attribute data from symbol metadata (works for both source and referenced assemblies)
        var attributeData = GetToolDefinitionAttributeData(symbol);
        if (attributeData == null)
        {
            return null;
        }

        IList<TypeDetails> toolTypes = FindToolInterface(symbol);
        if (toolTypes.Count <= 0)
        {
            return null;
        }

        // Get error codes from syntax if available (only for source files)
        ExpressionSyntax[] errorCodes = syntax != null ? FindToolExceptionErrorCodes(symbol) : [];

        return new ToolDetails
        {
            Syntax = syntax!,
            Symbol = symbol,
            Name = attributeData.Name,
            Title = attributeData.Title,
            Description = attributeData.Description,
            Usage = attributeData.Usage,
            Categories = attributeData.Categories,
            Keywords = attributeData.Keywords,
            ErrorCodes = ["ErrorCode.Unknown", ..errorCodes.Select(it => it.ToString())],
            Aliases = attributeData.Aliases,
            ArgsDetails = toolTypes[0],
            ResultDetails = toolTypes[1],
            ExtraTypes = toolTypes.SelectMany(it => it.Enums).GroupBy(it => it.Type).Select(it => it.First()).ToArray<TypeDeclaration>()
        };
    }
    
    /// <summary>
    /// Reads [System.ComponentModel.Description] from a symbol. Attribute arguments live in
    /// assembly metadata, so this works for referenced assemblies as well as source.
    /// </summary>
    private static string GetDescription(ISymbol symbol)
        => Normalize(symbol.GetAttributes()
            .FirstOrDefault(it => it.AttributeClass?.ToDisplayString() == "System.ComponentModel.DescriptionAttribute")
            ?.ConstructorArguments.FirstOrDefault().Value?.ToString());

    /// <summary>
    /// Normalizes authored text to LF so generated code does not depend on the
    /// line endings of the source file it was read from.
    /// </summary>
    internal static string Normalize(string? value)
        => value?.Replace("\r\n", "\n").Replace("\r", "\n") ?? string.Empty;

    /// <summary>
    /// Escapes text for emission inside a C# string literal.
    /// </summary>
    internal static string EscapeLiteral(string value)
        => value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");

    protected static bool HasConstructorMatchingProperties(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
        {
            return false;
        }

        var properties = namedType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        var constructors = namedType.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsImplicitlyDeclared)
            .ToList();

        // Check if there's a constructor with parameters matching all properties
        return constructors.Any(ctor =>
        {
            if (ctor.Parameters.Length != properties.Count)
            {
                return false;
            }

            for (int i = 0; i < ctor.Parameters.Length; i++)
            {
                var param = ctor.Parameters[i];
                var matchingProp = properties.FirstOrDefault(p =>
                    string.Equals(p.Name, param.Name, StringComparison.OrdinalIgnoreCase));

                if (matchingProp == null)
                {
                    return false;
                }
            }

            return true;
        });
    }

    private static ToolAttributeData? GetToolDefinitionAttributeData(INamedTypeSymbol symbol)
    {
        var attr = symbol.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString() == CodeDefinitions.ToolDefinitionAttribute.TypeFullName);

        if (attr == null)
        {
            return null;
        }

        string? name = null;
        string[] aliases = [];
        string[] categories = [];
        string[] keywords = [];
        var title = string.Empty;
        var description = string.Empty;
        var usage = string.Empty;

        // Extract from named arguments
        foreach (var namedArg in attr.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "Name":
                    name = namedArg.Value.Value?.ToString();
                    break;
                case "Aliases":
                    aliases = ExtractStringArray(namedArg.Value);
                    break;
                case "Categories":
                    categories = ExtractEnumArray(namedArg.Value);
                    break;
                case "Keywords":
                    keywords = ExtractEnumArray(namedArg.Value);
                    break;
                case "Title":
                    title = Normalize(namedArg.Value.Value?.ToString());
                    break;
                case "Description":
                    description = Normalize(namedArg.Value.Value?.ToString());
                    break;
                case "Usage":
                    usage = Normalize(namedArg.Value.Value?.ToString());
                    break;
            }
        }

        if (name == null)
        {
            return null;
        }

        return new ToolAttributeData(name, aliases, categories, keywords, title, description, usage);
    }

    private static string[] ExtractStringArray(TypedConstant constant)
    {
        if (constant.Kind != TypedConstantKind.Array)
        {
            return [];
        }

        // Add quotes around string values for code generation
        return constant.Values.Select(v => $"\"{v.Value?.ToString() ?? ""}\"").ToArray();
    }

    private static string[] ExtractEnumArray(TypedConstant constant)
    {
        if (constant.Kind != TypedConstantKind.Array)
        {
            return [];
        }

        return constant.Values
            .Where(v => v.Type is INamedTypeSymbol enumType && enumType.TypeKind == TypeKind.Enum)
            .Select(v =>
            {
                var enumType = (INamedTypeSymbol)v.Type!;
                var enumValue = v.Value;
                var enumMember = enumType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, enumValue));

                return enumMember != null
                    ? $"{enumType.Name}.{enumMember.Name}"
                    : enumValue?.ToString() ?? "";
            })
            .ToArray();
    }

    private class ToolAttributeData
    {
        public string Name { get; }
        public string[] Aliases { get; }
        public string[] Categories { get; }
        public string[] Keywords { get; }
        public string Title { get; }
        public string Description { get; }
        public string Usage { get; }

        public ToolAttributeData(string name, string[] aliases, string[] categories, string[] keywords, string title, string description, string usage)
        {
            Name = name;
            Aliases = aliases;
            Categories = categories;
            Keywords = keywords;
            Title = title;
            Description = description;
            Usage = usage;
        }
    }
    
    private static ExpressionSyntax[] FindToolExceptionErrorCodes(INamedTypeSymbol node) =>
        node.GetMembers()
            .OfType<IMethodSymbol>()
            .SelectMany(it => it.DeclaringSyntaxReferences)
            .Select(it => it.GetSyntax())
            .OfType<MethodDeclarationSyntax>()
            .SelectMany(it =>
                it.DescendantNodes()
                    .OfType<ThrowStatementSyntax>()
                    .Select(x => x.Expression)
                    .OfType<ObjectCreationExpressionSyntax>()
                    .Where(x => x.Type.ToFullString() == "ToolException")
                    .Select(x => x.ArgumentList?.Arguments[0].Expression!)
            )
            .ToArray();

    private static IList<TypeDetails> FindToolInterface(INamedTypeSymbol? typeSymbol)
    {
        INamedTypeSymbol? toolInterface = null;
        while (typeSymbol != null && toolInterface == null)
        {
            toolInterface = typeSymbol
                .Interfaces
                .FirstOrDefault(i => i.IsGenericType && i is { TypeArguments.Length: 2 });

            typeSymbol = typeSymbol.BaseType;
        }

        return toolInterface
            ?.TypeArguments
            .Select(arg =>
            {
                var defaults = GetConstructorDefaultValues(arg);
                return new TypeDetails
                {
                    Symbol = arg,
                    Type = arg.ToDisplayString(),
                    Properties =
                        arg.GetMembers()
                            .OfType<IPropertySymbol>()
                            .Where(it => it.DeclaredAccessibility == Accessibility.Public)
                            .Select(it => new PropertyDetails
                            {
                                Name = it.Name,
                                Type = it.Type.ToDisplayString(),
                                IsRequired = it.IsRequired,
                                IsNullable = it.NullableAnnotation == NullableAnnotation.Annotated,
                                IsPipeInput = it.GetAttributes().Any(a => a.AttributeClass?.Name == "PipeInputAttribute"),
                                IsPipeOutput = it.GetAttributes().Any(a => a.AttributeClass?.Name == "PipeOutputAttribute"),
                                Description = GetDescription(it),
                                DefaultValue = defaults.TryGetValue(it.Name, out var defVal) ? defVal : null
                            })
                            .ToArray(),
                    Enums = ExtractEnumsFromType(arg)
                };
            })
            .ToArray() ?? [];
    }

    private static Dictionary<string, string> GetConstructorDefaultValues(ITypeSymbol typeSymbol)
    {
        var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (typeSymbol is not INamedTypeSymbol namedType)
            return defaults;

        var ctor = namedType.Constructors
            .FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsImplicitlyDeclared);

        if (ctor == null) return defaults;

        foreach (var param in ctor.Parameters.Where(p => p.HasExplicitDefaultValue))
        {
            var defaultValue = param.ExplicitDefaultValue;

            // Unwrap Nullable<T> to detect underlying enum type
            ITypeSymbol paramType = param.Type;
            if (paramType is INamedTypeSymbol nt && nt.IsGenericType &&
                nt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                paramType = nt.TypeArguments[0];
            }

            string defaultStr;
            if (paramType is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType && defaultValue != null)
            {
                var enumMember = enumType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, defaultValue));
                defaultStr = enumMember != null
                    ? $"{enumType.ToDisplayString()}.{enumMember.Name}"
                    : (defaultValue?.ToString() ?? "default");
            }
            else
            {
                defaultStr = defaultValue switch
                {
                    null => "null",
                    bool b => b ? "true" : "false",
                    char c => $"'{c}'",
                    string s => $"\"{s}\"",
                    _ => defaultValue?.ToString() ?? "default"
                };
            }

            defaults[param.Name] = defaultStr;
        }

        return defaults;
    }

    private static EnumDetails[] ExtractEnumsFromType(ITypeSymbol typeSymbol)
    {
        var enums = new List<EnumDetails>();
        var processedEnums = new HashSet<string>();

        foreach (var prop in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (prop.Type is not INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
            {
                continue;
            }
            
            var enumFullName = enumType.ToDisplayString();

            if (enumType.ContainingNamespace.ToDisplayString() == "Dev.Tools" 
                && enumType.Name is "Category" or "Keyword" or "ErrorCode")
            {
                continue;
            }

            if (!processedEnums.Add(enumFullName))
            {
                continue;
            }

            enums.Add(new EnumDetails
            {
                Type = enumFullName,
                Values = enumType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(f => f.HasConstantValue)
                    .Select(f => new EnumValueDetails { Name = f.Name, Description = GetDescription(f) })
                    .ToArray()
            });
        }

        return enums.ToArray();
    }
    
    protected record ToolDetails : TypeDeclaration
    {
        public string Name { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Usage { get; set; } = string.Empty;
        public string[] Aliases { get; set; } = [];
        public string[] Categories { get; set; } = [];
        public string[] Keywords { get; set; } = [];
        public string[] ErrorCodes { get; set; } = [];
        public TypeDetails ArgsDetails { get; set; } = null!;
        public TypeDetails ResultDetails { get; set; } = null!;
        public IList<TypeDeclaration> ExtraTypes { get; set; } = [];
    }

    protected record TypeDetails : TypeDeclaration
    {
        public string Type { get; set; } = null!;
        public PropertyDetails[] Properties { get; set; } = [];
        public EnumDetails[] Enums { get; set; } = [];
    }

    protected record PropertyDetails
    {
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsRequired { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPipeInput { get; set; }
        public bool IsPipeOutput { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? DefaultValue { get; set; }
    }

    protected record EnumDetails : TypeDeclaration
    {
        public string Type { get; set; } = null!;
        public EnumValueDetails[] Values { get; set; } = [];
    }

    protected record EnumValueDetails
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
    }
}