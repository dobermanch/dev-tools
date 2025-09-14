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
        var syntax = symbol.DeclaringSyntaxReferences
            .Select(it => it.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();
        if (syntax == null)
        {
            return null;
        }
        
        Dictionary<string, ExpressionSyntax> values = GetToolDefinitionDetails(symbol);
        if (values.Count <= 0)
        {
            return null;
        }
        
        IList<TypeDetails> toolTypes = FindToolInterface(symbol);
        if (toolTypes.Count <= 0)
        {
            return null;
        }

        ExpressionSyntax[] errorCodes = FindToolExceptionErrorCodes(symbol);
        
        return new ToolDetails
        {
            Syntax = syntax,
            Symbol = symbol,
            Name = ((LiteralExpressionSyntax)values["Name"]).Token.ValueText,
            Categories = ((CollectionExpressionSyntax)values["Categories"]).Elements.Select(c => c.ToString()).ToArray(),
            Keywords = ((CollectionExpressionSyntax)values["Keywords"]).Elements.Select(c => c.ToString()).ToArray(),
            ErrorCodes = ["ErrorCode.Unknown", ..errorCodes.Select(it => it.ToString())],
            Aliases = ((CollectionExpressionSyntax)values["Aliases"]).Elements.Select(c => c.ToString()).ToArray(),
            ArgsDetails = toolTypes[0],
            ResultDetails = toolTypes[1]
        };
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
                        })
                        .ToArray()
            })
            .ToArray() ?? [];
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
    }

    protected record TypeDetails : TypeDeclaration
    {
        public string Type { get; set; } = null!;
        public PropertyDetails[] Properties { get; set; } = [];
    }
    
    protected record PropertyDetails
    {
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsRequired { get; set; }
        public bool IsNullable { get; set; }
    }
}