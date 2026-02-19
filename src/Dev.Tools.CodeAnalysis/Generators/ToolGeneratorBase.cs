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
            Categories = attributeData.Categories,
            Keywords = attributeData.Keywords,
            ErrorCodes = ["ErrorCode.Unknown", ..errorCodes.Select(it => it.ToString())],
            Aliases = attributeData.Aliases,
            ArgsDetails = toolTypes[0],
            ResultDetails = toolTypes[1],
            ExtraTypes = toolTypes.SelectMany(it => it.Enums).GroupBy(it => it.Type).Select(it => it.First()).ToArray<TypeDeclaration>()
        };
    }
    
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
            }
        }

        if (name == null)
        {
            return null;
        }

        return new ToolAttributeData(name, aliases, categories, keywords);
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

        public ToolAttributeData(string name, string[] aliases, string[] categories, string[] keywords)
        {
            Name = name;
            Aliases = aliases;
            Categories = categories;
            Keywords = keywords;
        }
    }
    
    private static Dictionary<string, ExpressionSyntax> GetToolDefinitionDetails(INamedTypeSymbol node) =>
        node.GetAttributes()
            .Where(x => x.AttributeClass?.ToDisplayString() == CodeDefinitions.ToolDefinitionAttribute.TypeFullName)
            .Select(it => it.ApplicationSyntaxReference)
            .Select(it => it?.GetSyntax())
            .OfType<AttributeSyntax>()
            .SelectMany(it => it.ArgumentList?.Arguments ?? [])
            .Select(it => (it.NameEquals?.Name.Identifier.Text, it.Expression))
            .Where(it => it.Text != null)
            .ToDictionary(it => it.Text!, it => it.Expression);
    
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
            .Select(arg => new TypeDetails
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
                        })
                        .ToArray(),
                Enums = ExtractEnumsFromType(arg)
            })
            .ToArray() ?? [];
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
                    .Select(f => f.Name)
                    .ToArray()
            });
        }

        return enums.ToArray();
    }
    
    protected record ToolDetails : TypeDeclaration
    {
        public string Name { get; set; } = null!;
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
    }

    protected record EnumDetails : TypeDeclaration
    {
        public string Type { get; set; } = null!;
        public string[] Values { get; set; } = [];
    }
}