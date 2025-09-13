using System.Collections.Immutable;
using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.CodeAnalysis.Generators;

[Generator]
public class ToolsCatalogGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(CodeDefinitions.ToolDefinitionAttribute.OutputFileName,
                CodeDefinitions.ToolDefinitionAttribute);
            ctx.AddSource(CodeDefinitions.ToolDefinition.OutputFileName, CodeDefinitions.ToolDefinition);
        });

        var classDeclarations = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsToolsClass(s),
                transform: (ctx, _) => GetToolDetails(ctx))
            .Collect();

        context.RegisterSourceOutput(classDeclarations, GenerateToolCatalog);
    }

    private static bool IsToolsClass(SyntaxNode node) =>
        node is ClassDeclarationSyntax definition
        && definition.HasAttribute(CodeDefinitions.ToolDefinitionAttribute.SyntaxTypeName);

    private static ToolDetails? GetToolDetails(GeneratorSyntaxContext context)
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

    private static Dictionary<string, ExpressionSyntax> GetToolDefinitionDetails(GeneratorSyntaxContext context) =>
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

    private static ExpressionSyntax[] FindToolExceptionErrorCodes(TypeDeclarationSyntax node) =>
        node
            .DescendantNodes()
            .OfType<ThrowStatementSyntax>()
            .Select(it => it.Expression)
            .OfType<ObjectCreationExpressionSyntax>()
            .Where(it => it.Type.ToFullString() == "ToolException")
            .Select(it => it.ArgumentList?.Arguments[0].Expression!)
            .ToArray();

    private static INamedTypeSymbol? FindToolInterface(INamedTypeSymbol? typeSymbol)
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

    private static void GenerateToolCatalog(SourceProductionContext spc, ImmutableArray<ToolDetails?> source)
    {
        ToolDetails[] tools = source
            .Where(it => it != null)
            .Select(it => it!)
            .ToArray();

        if (tools.Length == 0)
        {
            return;
        }

        string Aggregate(IList<string> data)
        {
            return "[" + string.Join(", ", data) + "]";
        }

        string toolsString = tools.Select(it =>
            $"""
                     new ToolDefinition(
                         Name: "{it.Name}",
                         Aliases: {Aggregate(it.Aliases)},
                         Categories: {Aggregate(it.Categories)},
                         Keywords: {Aggregate(it.Keywords)},
                         ErrorCodes: {Aggregate(it.ErrorCodes)},
                         ToolType: typeof({it.Namespace}.{it.TypeName}),
                         ArgsType: new ToolTypeDetails(
                             DataType: typeof({it.ArgsDetails.Type}),
                             Properties: [
                                 {string.Join(",\n\t\t\t\t\t", it.ArgsDetails.Properties.Select(x => $"new ToolTypeProperty(\"{x.Name}\", typeof({x.Type.Replace("?", "")}), {x.IsRequired.ToString().ToLower()}, {x.IsNullable.ToString().ToLower()})"))}
                             ]
                         ),
                         ReturnType: new ToolTypeDetails(
                             DataType: typeof({it.ResultDetails.Type}),
                             Properties: [
                                 {string.Join(",\n\t\t\t\t\t", it.ResultDetails.Properties.Select(x => $"new ToolTypeProperty(\"{x.Name}\", typeof({x.Type.Replace("?", "")}), {x.IsRequired.ToString().ToLower()}, {x.IsNullable.ToString().ToLower()})"))}
                             ]
                         )   
                     ),
             """).Aggregate("", (current, it) => current + it + "\n");

        var codeBlock = new CodeBlock
        {
            Namespace = "Dev.Tools",
            TypeName = "ToolsCatalog",
            GeneratorType = typeof(ToolsCatalogGenerator),
            Placeholders = new()
            {
                ["Tools"] = toolsString
            },
            Usings =
            [
                "System.Collections.Generic"
            ],
            Content =
                """
                internal class {TypeName}
                {
                    public static IReadOnlyCollection<ToolDefinition> ToolDefinitions { get; } = 
                    [
                {Tools}
                    ];
                }
                """
        };

        spc.AddSource(codeBlock.OutputFileName, codeBlock);
    }

    private record ToolDetails : TypeDeclaration
    {
        public string Name { get; set; } = null!;
        public string[] Aliases { get; set; } = [];
        public string[] Categories { get; set; } = [];
        public string[] Keywords { get; set; } = [];
        public string[] ErrorCodes { get; set; } = [];
        public TypeDetails ArgsDetails { get; set; } = null!;
        public TypeDetails ResultDetails { get; set; } = null!;
    }

    private record TypeDetails : TypeDeclaration
    {
        public string Type { get; set; } = null!;
        public PropertyDetails[] Properties { get; set; } = [];
    }
    
    record PropertyDetails
    {
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsRequired { get; set; }
        public bool IsNullable { get; set; }
    }
}