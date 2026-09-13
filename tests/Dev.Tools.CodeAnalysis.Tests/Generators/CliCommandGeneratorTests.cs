using Dev.Tools.CodeAnalysis.Generators;

namespace Dev.Tools.CodeAnalysis.Tests.Generators;

public class CliCommandGeneratorTests : GeneratorTestsBase
{
    [Test]
    public async Task ShouldGenerateACommandPerToolPlusRegistrationAndDescriptions()
    {
        await Verify<CliCommandGenerator>(
            ConsumerAssembly("Dev.Tools.Console.Core", "GenerateToolsCliCommandAttribute"),
            ToolsAssembly);
    }

    [Test]
    public async Task ShouldGenerateNothingWithoutTheAssemblyAttribute()
    {
        await VerifyNoOutput<CliCommandGenerator>(EmptyAssembly, ToolsAssembly);
    }
}
