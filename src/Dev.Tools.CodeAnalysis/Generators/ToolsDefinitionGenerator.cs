using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.CodeAnalysis.Generators;

[Generator]
public class ToolsDefinitionGenerator : IIncrementalGenerator
{
    public static readonly CodeBlock Attribute = new()
    {
        Namespace = "Dev.Tools",
        TypeName = "ToolDefinitionAttribute",
        SyntaxTypeName = "ToolDefinition",
        GeneratorType = typeof(ToolsDefinitionGenerator),
        Content = """
                  [AttributeUsage(AttributeTargets.Class)]
                  internal sealed class {TypeName} : System.Attribute
                  {
                      public string Name { get; set; } = default!;
                      public string[] Aliases { get; set; } = [];
                      public string[] Categories { get; set; } = [];
                      public string[] Keywords { get; set; } = [];
                      public string[] ErrorCodes { get; set; } = [];
                  }
                  """
    };
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource(Attribute.OutputFileName, Attribute)
        );

        // Register a syntax receiver to filter syntax nodes
        var classDeclarations = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsClassWithGenerateValuesAttribute(s),
                transform: (ctx, _) => GetClassWithConstants(ctx))
            .Where(c => c != null)
            .Collect();

        var compilationAndClasses = context
            .CompilationProvider
            .Combine(classDeclarations);

        context.RegisterSourceOutput(compilationAndClasses, (spc, source) =>
        {
            var (_, classes) = source;

            var types = classes
                .Where(it => it != null)
                .Select(it => it!)
                .ToArray();

            if (types.Length == 0)
            {
                return;
            }

            var codeBlock = GenerateGetValuesMethod(classes);
            spc.AddSource(codeBlock.OutputFileName, codeBlock);
        });
    }

    private static bool IsClassWithGenerateValuesAttribute(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax definition 
               && definition.HasAttribute(Attribute.SyntaxTypeName);
    }

    private static TypeInfo? GetClassWithConstants(GeneratorSyntaxContext context)
    {
        var (syntax, symbol) = context.Node.GetTypeNode(context.SemanticModel);
        if (syntax is null || symbol is null)
        {
            return null;
        }
        
        var attribute = context.Node.GetAttributeSyntax(context.SemanticModel, Attribute.TypeFullName);
        Dictionary<string, ExpressionSyntax>? values =
            attribute
                ?.ArgumentList
                ?.Arguments
                .Select(it => (it.NameEquals?.Name.Identifier.Text, it.Expression))
                .Where(it => it.Item1 != null)
                .ToDictionary(it => it.Item1!, it => it.Item2);
        
        if (values is null)
        {
            return null;
        }

        return new TypeInfo
        {
            Syntax = syntax,
            Symbol = symbol,
            Name = ((LiteralExpressionSyntax)values["Name"]).Token.ValueText,
            Categories = ((CollectionExpressionSyntax)values["Categories"]).Elements.Select(c => c.ToString())
                .ToArray(),
            Keywords = ((CollectionExpressionSyntax)values["Keywords"]).Elements.Select(c => c.ToString()).ToArray(),
            ErrorCodes = ((CollectionExpressionSyntax)values["ErrorCodes"]).Elements.Select(c => c.ToString())
                .ToArray(),
            Aliases = ((CollectionExpressionSyntax)values["Aliases"]).Elements.Select(c => c.ToString()).ToArray(),
        };
    }

    private static CodeBlock GenerateGetValuesMethod(IList<TypeInfo> infos)
    {
        string Aggregate(IList<string> data)
        {
            return "[" + string.Join(", ", data) + "]";
        }

        return new CodeBlock
        {
            Namespace = "Dev.Tools.Providers",
            TypeName = "ToolsProvider",
            GeneratorType = typeof(ToolsDefinitionGenerator),
            Placeholders = new()
            {
                ["Tools"] = infos.Select(it =>
                    $"""
                             yield return new ToolDefinition(
                                 Name: "{it.Name}",
                                 Aliases: {Aggregate(it.Aliases)},
                                 Categories: {Aggregate(it.Categories)},
                                 Keywords: {Aggregate(it.Keywords)},
                                 ErrorCodes: {Aggregate(it.ErrorCodes)},
                                 ToolType: typeof({it.Namespace}.{it.TypeName})
                             );
                     """).Aggregate("", (current, it) => current + it + "\n")
            },
            Content = """
                      internal partial class {TypeName}
                      {
                          private IEnumerable<ToolDefinition> GetToolDefinitions()
                          {
                      {Tools}
                          }
                      }
                      """
        };
    }

    private record TypeInfo : TypeDeclaration
    {
        public string Name { get; set; } = default!;
        public string[] Aliases { get; set; } = [];
        public string[] Categories { get; set; } = [];
        public string[] Keywords { get; set; } = [];
        public string[] ErrorCodes { get; set; } = [];
        public TypeDetails? Args { get; set; }
        public TypeDetails? Result { get; set; }

        public record TypeDetails : TypeDeclaration
        {
            public (string Name, string Type)[] Properties { get; set; } = [];
        }
    }
}