using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.CodeAnalysis.Core;

internal static class GeneratorExtensions
{
    public static IEnumerable<ITypeSymbol> GetAllTypes(this INamespaceSymbol root,
        Predicate<ITypeSymbol>? predicate = null)
    {
        foreach (var namespaceOrTypeSymbol in root.GetMembers())
        {
            if (namespaceOrTypeSymbol is INamespaceSymbol @namespace)
            {
                foreach (var nested in GetAllTypes(@namespace, predicate))
                {
                    yield return nested;
                }
            }
            else if (namespaceOrTypeSymbol is ITypeSymbol type)
            {
                if (predicate is null || predicate(type))
                {
                    yield return type;
                }
            }
        }
    }

    public static bool HasAttribute(this SyntaxNode node, string attributeName)
    {
        return node is TypeDeclarationSyntax declaration
               && declaration.AttributeLists
                   .SelectMany(it => it.Attributes)
                   .Any(it => it.Name.ToString() == attributeName);
    }

    public static AttributeSyntax? GetAttributeSyntax(this SyntaxNode node, SemanticModel model, string attributeName)
    {
        return model
            .GetDeclaredSymbol(node)
            ?.GetAttributeSyntax(attributeName);
    }

    public static AttributeSyntax? GetAttributeSyntax(this ISymbol symbol, string attributeName)
        => symbol.GetAttributeData(attributeName)
            ?.ApplicationSyntaxReference
            ?.GetSyntax() as AttributeSyntax;

    public static bool HasAttribute(this ISymbol symbol, string attributeName)
        => symbol.GetAttributeData(attributeName) != null;

    public static AttributeData? GetAttributeData(this ISymbol symbol, string attributeName)
        => symbol
            .GetAttributes()
            .FirstOrDefault(it => it.AttributeClass?.ToString() == attributeName);

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