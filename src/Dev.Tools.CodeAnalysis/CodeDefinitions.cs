using Dev.Tools.CodeAnalysis.Core;
using Dev.Tools.CodeAnalysis.Generators;

namespace Dev.Tools.CodeAnalysis;

public static class CodeDefinitions
{
    public static readonly string ToolsAssemblyName = "Dev.Tools";
    public static readonly string ToolsGlobalNamespace = "Dev.Tools";
        
    public static readonly CodeBlock ToolDefinitionAttribute = new()
    {
        Namespace = ToolsGlobalNamespace,
        TypeName = "ToolDefinitionAttribute",
        SyntaxTypeName = "ToolDefinition",
        GeneratorType = typeof(ToolsCatalogGenerator),
        Usings = ["System"],
        Content = """
                  [AttributeUsage(AttributeTargets.Class)]
                  internal sealed class {TypeName} : Attribute
                  {
                      public required string Name { get; set; }
                      public string[] Aliases { get; set; } = [];
                      public Category[] Categories { get; set; } = [];
                      public Keyword[] Keywords { get; set; } = [];
                  }
                  """
    };
    
    public static readonly CodeBlock Category = new()
    {
        Namespace = ToolsGlobalNamespace,
        TypeName = "Category",
        Usings = ["System"],
        Content = """
                  public enum Category
                  {
                      Value1,
                      Value2
                  }
                  """
    };
    
    public static readonly CodeBlock Keywords = new()
    {
        Namespace = ToolsGlobalNamespace,
        TypeName = "Keyword",
        Usings = ["System"],
        Content = """
                  public enum Keyword
                  {
                      Value1,
                      Value2
                  }
                  """
    };
    
    public static readonly CodeBlock ToolDefinition = new()
    {
        Namespace = ToolsGlobalNamespace,
        TypeName = "ToolDefinition",
        SyntaxTypeName = "ToolDefinition",
        GeneratorType = typeof(ToolsCatalogGenerator),
        Content = """
                  public sealed record ToolDefinition
                  (
                      string Name,
                      string[] Aliases,
                      Category[] Categories,
                      Keyword[] Keywords,
                      string[] ErrorCodes,
                      Type ToolType,
                      ToolTypeDetails ArgsType,
                      ToolTypeDetails ReturnType
                  );
                  
                  public sealed record ToolTypeDetails(
                    Type DataType,
                    ToolTypeProperty[] Properties
                  );
                  
                  public sealed record ToolTypeProperty(string Name, Type PropertyType, bool IsRequired, bool IsNullable);
                  """
    };
    
    // public static readonly CodeBlock GenerateApiEndpointsAttribute = new()
    // {
    //     Namespace = ToolsGlobalNamespace,
    //     TypeName = "GenerateApiEndpointsAttribute",
    //     SyntaxTypeName = "GenerateApiEndpoints",
    //     GeneratorType = typeof(ToolsCatalogGenerator),
    //     Usings = ["System"],
    //     Content = """
    //               [AttributeUsage(AttributeTargets.Assembly)]
    //               internal class {TypeName} : Attribute;
    //               """
    // };
    
    public static readonly CodeBlock GenerateCommandsAttribute = new()
    {
        Namespace = ToolsGlobalNamespace,
        TypeName = "GenerateCommandsAttribute",
        SyntaxTypeName = "GenerateCommands",
        GeneratorType = typeof(ToolsCatalogGenerator),
        Usings = ["System"],
        Content = """
                  [AttributeUsage(AttributeTargets.Assembly)]
                  internal class {TypeName} : Attribute;
                  """
    };
    
    public static readonly CodeBlock GenerateMcpToolsAttribute = new()
    {
        Namespace = ToolsGlobalNamespace,
        TypeName = "GenerateMcpToolsAttribute",
        SyntaxTypeName = "GenerateMcpTools",
        GeneratorType = typeof(ToolsCatalogGenerator),
        Usings = ["System"],
        Content = """
                  [AttributeUsage(AttributeTargets.Assembly)]
                  internal class {TypeName} : Attribute;
                  """
    };
    
    public static readonly CodeBlock GenerateValuesAttribute = new ()
    {
        Namespace = ToolsGlobalNamespace,
        TypeName = "GenerateValuesAttribute",
        SyntaxTypeName = "GenerateValues",
        Usings = ["System"],
        Content = """
                  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
                  internal class {TypeName}: Attribute;
                  """
    };
    
    public static readonly CodeBlock ToolsArgs = new ()
    {
        Namespace = ToolsGlobalNamespace + ".Core",
        TypeName = "ToolArgs",
        Content = """
                  public record ToolArgs;
                  """
    };
    
    public static readonly CodeBlock ToolsResult = new ()
    {
        Namespace = ToolsGlobalNamespace + ".Core",
        TypeName = "ToolResult",
        Content = """
                  public record ToolResult;
                  """
    };
}