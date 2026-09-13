using Dev.Tools.CodeAnalysis.Generators;

namespace Dev.Tools.CodeAnalysis.Tests.Generators;

public class ApiEndpointGeneratorTests : GeneratorTestsBase
{
    [Test]
    public async Task ShouldGenerateAnEndpointPerTool()
    {
        await Verify<ApiEndpointGenerator>(
            ConsumerAssembly("Dev.Tools.Api.Core", "GenerateToolsApiEndpointAttribute"),
            ToolsAssembly);
    }

    [Test]
    public async Task ShouldGenerateNothingWithoutTheAssemblyAttribute()
    {
        await VerifyNoOutput<ApiEndpointGenerator>(EmptyAssembly, ToolsAssembly);
    }
}
