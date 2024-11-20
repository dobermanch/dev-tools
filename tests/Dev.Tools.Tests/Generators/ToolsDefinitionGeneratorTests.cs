using Dev.Tools.Generators;
using Dev.Tools.Generators.Core;

namespace Dev.Tools.Tests.Generators;

public class ToolsDefinitionGeneratorTests : GeneratorTestsBase
{
    [Fact]
    public async Task Should_Generate_Tool_Provider()
    {
        var code = new CodeBlock
        {
            Namespace = "Dev.Tools",
            TypeName = "TestTool",
            Header = "",
            GeneratorType = typeof(ToolsDefinitionGenerator),
            Content = """
                      [ToolDefinition(
                          Name = "tool-test",
                          Aliases = ["tt"],
                          Keywords = ["test", "keywords"],
                          Categories = ["default"],
                          ErrorCodes = ["unknown"]
                      )]
                      public sealed class TestTool {}
                      """
        };
        await Verify<ToolsDefinitionGenerator>(code);
    }
}