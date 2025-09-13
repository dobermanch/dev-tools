using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.CodeAnalysis.Generators;

public class ToolGeneratorBase
{
    protected static ToolDetails? GetToolDetails(GeneratorSyntaxContext context)
    {
        (TypeDeclarationSyntax? syntax, INamedTypeSymbol? symbol) = context.Node.GetTypeNode(context.SemanticModel);
        if (syntax is null || symbol is null)
        {
            return null;
        }

        Dictionary<string, ExpressionSyntax> values = GetToolDefinitionDetails(context);
        if (values.Count <= 0)
        {
            return null;
        }
        
        INamedTypeSymbol? toolInterface = FindToolInterface(symbol);
        if (toolInterface is null)
        {
            return null;
        }

        TypeDetails[] toolTypes = toolInterface
            .TypeArguments
            .Select(arg => new TypeDetails
            {
                Type = arg.ToDisplayString(),
                Properties = arg.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(it => it.DeclaredAccessibility == Accessibility.Public)
                    .Select(it => new PropertyDetails{
                        Name = it.Name, 
                        Type = it.Type.ToDisplayString(),
                        IsRequired = it.IsRequired,
                        IsNullable = it.NullableAnnotation == NullableAnnotation.Annotated,
                    })
                    .ToArray()
            })
            .ToArray();

        ExpressionSyntax[] errorCodes = FindToolExceptionErrorCodes(syntax);
        
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

    protected static Dictionary<string, ExpressionSyntax> GetToolDefinitionDetails(GeneratorSyntaxContext context) =>
        context
            .Node
            .GetAttributeSyntax(
                context.SemanticModel,
                CodeDefinitions.ToolDefinitionAttribute.TypeFullName
            )
            ?.ArgumentList
            ?.Arguments
            .Select(it => (it.NameEquals?.Name.Identifier.Text, it.Expression))
            .Where(it => it.Item1 != null)
            .ToDictionary(it => it.Item1!, it => it.Item2) ?? [];

    protected static ExpressionSyntax[] FindToolExceptionErrorCodes(TypeDeclarationSyntax node) =>
        node
            .DescendantNodes()
            .OfType<ThrowStatementSyntax>()
            .Select(it => it.Expression)
            .OfType<ObjectCreationExpressionSyntax>()
            .Where(it => it.Type.ToFullString() == "ToolException")
            .Select(it => it.ArgumentList?.Arguments[0].Expression!)
            .ToArray();

    protected static INamedTypeSymbol? FindToolInterface(INamedTypeSymbol? typeSymbol)
    {
        while (typeSymbol != null)
        {
            INamedTypeSymbol? toolInterface = typeSymbol
                .Interfaces
                .FirstOrDefault(i => i.IsGenericType && i is { TypeArguments.Length: 2 });
            
            if (toolInterface != null)
            {
                return toolInterface;
            }

            typeSymbol = typeSymbol.BaseType;
        }

        return null;
    }
    
    protected  record ToolDetails : TypeDeclaration
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