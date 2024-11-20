using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.CodeAnalysis.Core;

internal static class GeneratorExtensions {

    public static bool HasAttribute(this SyntaxNode node, string attributeName)
    {
        return node is TypeDeclarationSyntax declaration
               && declaration.AttributeLists
                   .SelectMany(it => it.Attributes)
                   .Any(it => it.Name.ToString() == attributeName);
    }
    
    public static AttributeSyntax? GetAttributeSyntax(this SyntaxNode node, SemanticModel model, string attributeName)
    {
        var attribute = model
            .GetDeclaredSymbol(node)
            ?.GetAttributes()
            .FirstOrDefault(it => it.AttributeClass?.ToString() == attributeName);
        
        return attribute?.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
    }

    public static (TypeDeclarationSyntax?, INamedTypeSymbol?) GetTypeNode(this SyntaxNode node, SemanticModel model)
    {
        var classDeclaration = node as TypeDeclarationSyntax;
        if (classDeclaration is null || model.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
        {
            return (classDeclaration, null);
        }
        
        return (classDeclaration, classSymbol);
    }
}