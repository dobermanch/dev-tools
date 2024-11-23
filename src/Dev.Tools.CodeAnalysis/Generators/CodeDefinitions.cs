using Dev.Tools.CodeAnalysis.Core;

namespace Dev.Tools.CodeAnalysis.Generators;

public static class CodeDefinitions
{
    public static readonly string ToolsAssemblyName = "Dev.Tools";
    public static readonly string ToolsGlobalNamespace = "Dev.Tools";
        
    public static readonly CodeBlock ToolDefinitionAttribute = new()
    {
        Namespace = ToolsGlobalNamespace,
        TypeName = "ToolDefinitionAttribute",
        SyntaxTypeName = "ToolDefinition",
        Usings = ["System"],
        Content = """
                  [AttributeUsage(AttributeTargets.Class)]
                  internal sealed class {TypeName} : Attribute
                  {
                      public string Name { get; set; } = default!;
                      public string[] Aliases { get; set; } = [];
                      public string[] Categories { get; set; } = [];
                      public string[] Keywords { get; set; } = [];
                      public string[] ErrorCodes { get; set; } = [];
                  }
                  """
    };
    
    public static readonly CodeBlock GenerateApiEndpointsAttribute = new()
    {
        Namespace = ToolsGlobalNamespace,
        TypeName = "GenerateApiEndpointsAttribute",
        SyntaxTypeName = "GenerateApiEndpoints",
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