using Dev.Tools.CodeAnalysis.Core;
using Dev.Tools.CodeAnalysis.Generators;

namespace Dev.Tools.CodeAnalysis.Tests.Generators;

public class ToolsCatalogGeneratorTests : GeneratorTestsBase
{
    [Test]
    public async Task Should_Generate_Tools_Provider()
    {
        var code = new CodeBlock
        {
            Namespace = "Dev.Tools",
            TypeName = "TestTool",
            Header = "",
            GeneratorType = typeof(ToolsCatalogGenerator),
            Content = """
                      [ToolDefinition(
                          Name = "tool-test",
                          Aliases = ["tt"],
                          Keywords = ["test", "keywords"],
                          Categories = ["default"]
                      )]
                      public sealed class TestTool : ITool<int, long> {
                        public void Test1() {
                            throw new ToolException("testCode"); 
                        }
                        public void Test2() {
                          throw new ToolException(Errors.TextEmpty); 
                        }
                      }
                      
                      public interface ITool<TArgs, TResult> {}
                      
                      public class Errors {
                        public const string TextEmpty = "TextEmpty";
                      }
                      
                      public sealed class ToolException(string errorCode) : Exception
                      {
                          public string ErrorCode { get; } = errorCode;
                      }
                      """
        };
        await Verify<ToolsCatalogGenerator>(code);
    }
    
    //[Test]
    public async Task Should_Properly_Cache_Results()
    {
        var code = new CodeBlock
        {
            Namespace = "Dev.Tools",
            TypeName = "TestTool",
            Header = "",
            GeneratorType = typeof(ToolsCatalogGenerator),
            Content = """
                      [ToolDefinition(
                          Name = "tool-test",
                          Aliases = ["tt"],
                          Keywords = ["test", "keywords"],
                          Categories = ["default"]
                      )]
                      public sealed class TestTool : ITool<int, long> {
                        public void Test1() {
                            throw new ToolException("testCode"); 
                        }
                        public void Test2() {
                          throw new ToolException(Errors.TextEmpty); 
                        }
                      }

                      public interface ITool<TArgs, TResult> {}

                      public class Errors {
                        public const string TextEmpty = "TextEmpty";
                      }

                      public sealed class ToolException(string errorCode) : Exception
                      {
                          public string ErrorCode { get; } = errorCode;
                      }
                      """
        };
        
        await VerifyCaching<ToolsCatalogGenerator, ToolsCatalogGenerator.TrackingNames>(code);
    }
}