using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.CodeAnalysis.Core;

public record TypeDeclaration
{
    public TypeDeclarationSyntax Syntax { get; set; } = null!;

    public ITypeSymbol Symbol { get; set; } = null!;

    public string Namespace => Symbol.ContainingNamespace.ToDisplayString();

    public string TypeName => Symbol.Name;

    public bool IsClass => Symbol.IsRecord;

    public bool IsValueType => Symbol.IsValueType;

    public bool IsRecord => Symbol.IsRecord;

    public bool IsPartial => Syntax.Modifiers.Any(it => it.IsKind(SyntaxKind.PartialKeyword));

    public bool IsPublic => Syntax.Modifiers.Any(it => it.IsKind(SyntaxKind.PublicKeyword));

    public bool IsSealed => Syntax.Modifiers.Any(it => it.IsKind(SyntaxKind.SealedKeyword));

    public Location Location => Syntax.GetLocation();
}