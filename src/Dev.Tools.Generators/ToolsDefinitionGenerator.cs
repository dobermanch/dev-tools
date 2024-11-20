using Dev.Tools.Generators.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.Generators;

[Generator]
public class ToolsDefinitionGenerator : IIncrementalGenerator
{
    private static readonly CodeBlock AttributeCode = new()
    {
        Namespace = "Dev.Tools",
        TypeName = "ToolDefinitionAttribute",
        SyntaxTypeName = "ToolDefinition",
        GeneratorType = typeof(ToolsDefinitionGenerator),
        Content = """
                  [AttributeUsage(AttributeTargets.Class)]
                  public sealed class {TypeName} : Attribute
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
            ctx.AddSource(AttributeCode.OutputFileName, AttributeCode)
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

            var notValid = types.Where(it => !it.IsPublic || !it.IsSealed).ToArray();
            if (notValid.Length <= 0)
            {
                var codeBlock = GenerateGetValuesMethod(classes);
                spc.AddSource(codeBlock.OutputFileName, codeBlock);
                return;
            }

            foreach (var info in notValid)
            {
                spc.ReportDiagnostic(
                    Diagnostic.Create(Diagnostics.NotPublicRule,
                        info.Location,
                        info.TypeName,
                        AttributeCode.TypeFullName));
            }
        });
    }

    private static bool IsClassWithGenerateValuesAttribute(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax definition 
               && definition.HasAttribute(AttributeCode.SyntaxTypeName);
    }

    private static TypeInfo? GetClassWithConstants(GeneratorSyntaxContext context)
    {
        var (syntax, symbol) = context.GetTypeNode();
        if (symbol is null)
        {
            return null;
        }
        
        var attribute = context.Node.GetAttributeSyntax(context, AttributeCode.TypeFullName);
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
                          protected IEnumerable<ToolDefinition> GetToolDefinitions()
                          {
                      {Tools}
                          }
                      }
                      """
        };
    }

    private record TypeInfo : TypeBaseInfo
    {
        public string Name { get; set; } = default!;
        public string[] Aliases { get; set; } = [];
        public string[] Categories { get; set; } = [];
        public string[] Keywords { get; set; } = [];
        public string[] ErrorCodes { get; set; } = [];
    }
}