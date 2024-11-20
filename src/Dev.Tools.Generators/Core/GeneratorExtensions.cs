using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.Generators.Core;

internal static class GeneratorExtensions {

    public static bool HasAttribute(this SyntaxNode node, string attributeName)
    {
        return node is TypeDeclarationSyntax declaration
               && declaration.AttributeLists
                   .SelectMany(it => it.Attributes)
                   .Any(it => it.Name.ToString() == attributeName);
    }
    
    
    public static AttributeSyntax? GetAttributeSyntax(this SyntaxNode node, GeneratorSyntaxContext context, string attributeName)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(node);

        var attribute = symbol
            ?.GetAttributes()
            .FirstOrDefault(it => it.AttributeClass?.ToString() == attributeName);
        
        return attribute?.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
    }

    public static (TypeDeclarationSyntax, INamedTypeSymbol?) GetTypeNode(this GeneratorSyntaxContext context)
    {
        var classDeclaration = (TypeDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
        {
            return (classDeclaration, null);
        }
        
        return (classDeclaration, classSymbol);
    }
}