using Dev.Tools.Providers;
using Dev.Tools.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace Dev.Tools.Tests;

public class ToolsProviderTests
{
    private static IToolsProvider Provider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        services.AddDevTools();

        return services.BuildServiceProvider().GetRequiredService<IToolsProvider>();
    }

    [Test]
    public async Task ShouldFindADefinitionByItsAlias()
    {
        var provider = Provider();

        await Assert.That(provider.GetToolDefinition("64e")).IsSameReferenceAs(provider.GetToolDefinition("base64-encoder"));
    }

    [Test]
    public async Task ShouldFindADefinitionByType()
    {
        var provider = Provider();

        await Assert.That(provider.GetToolDefinition(typeof(HashTextTool)).Name).IsEqualTo("hash");
    }

    [Test]
    public async Task ShouldResolveAToolByNameAliasOrType()
    {
        var provider = Provider();

        await Assert.That(provider.GetTool("base64-encoder")).IsTypeOf<Base64EncoderTool>();
        await Assert.That(provider.GetTool("64e")).IsTypeOf<Base64EncoderTool>();
        await Assert.That(provider.GetTool(typeof(Base64EncoderTool))).IsTypeOf<Base64EncoderTool>();
    }

    [Test]
    public async Task AToolResolvedByNameShouldRun()
    {
        // The point of the non-generic lookups: a host that only has a name can still run the tool.
        var tool = (ITool<Base64EncoderTool.Args, Base64EncoderTool.Result>)Provider().GetTool("64e");

        var result = await tool.RunAsync(new Base64EncoderTool.Args("hello"), CancellationToken.None);

        await Assert.That(result.Text).IsEqualTo("aGVsbG8=");
    }

    [Test]
    public async Task ShouldResolveToolsThroughTheContainer()
    {
        // A host substitutes a dependency by registering it; the provider has to honour that rather
        // than building the tool itself.
        var substitute = new CountingMd5Hash();
        var provider = Provider(services => services.AddSingleton<Dev.Tools.Cryptography.IMd5Hash>(substitute));

        var tool = provider.GetTool<HashTextTool>();
        await tool.RunAsync(new HashTextTool.Args("hello"), CancellationToken.None);

        await Assert.That(substitute.Calls).IsEqualTo(1);
    }

    [Test]
    [Arguments("no-such-tool")]
    [Arguments("")]
    public async Task ShouldThrowForAnUnknownNameOrAlias(string nameOrAlias)
    {
        var provider = Provider();

        await Assert.That(() => provider.GetToolDefinition(nameOrAlias)).Throws<ToolDefinitionNotFoundException>();
        await Assert.That(() => provider.GetTool(nameOrAlias)).Throws<ToolDefinitionNotFoundException>();
    }

    [Test]
    public async Task ShouldThrowForATypeThatIsNotATool()
    {
        var provider = Provider();

        await Assert.That(() => provider.GetTool(typeof(string))).Throws<ToolDefinitionNotFoundException>();
    }

    [Test]
    public async Task NamesAndAliasesShouldBeUniqueAcrossTheCatalog()
    {
        var duplicated = ToolsCatalog.ToolDefinitions
            .SelectMany(tool => new[] { tool.Name }.Concat(tool.Aliases))
            .GroupBy(it => it)
            .Where(it => it.Count() > 1)
            .Select(it => it.Key)
            .ToArray();

        await Assert.That(duplicated).IsEmpty();
    }

    [Test]
    public async Task ACollisionShouldBeReportedByName()
    {
        var hash = ToolsCatalog.ToolDefinitions.Single(it => it.Name == "hash");
        var encoder = ToolsCatalog.ToolDefinitions.Single(it => it.Name == "base64-encoder");
        var clashing = encoder with { Aliases = ["hash"] };

        var exception = await Assert.That(() => new ToolsProvider(new ServiceCollection().BuildServiceProvider(), [hash, clashing]))
            .Throws<InvalidOperationException>();

        await Assert.That(exception!.Message).Contains("'hash' (hash, base64-encoder)");
    }

    private sealed class CountingMd5Hash : Dev.Tools.Cryptography.IMd5Hash
    {
        private readonly Dev.Tools.Cryptography.Md5Hash _inner = new();

        public int Calls { get; private set; }

        public byte[] ComputeHash(byte[] data)
        {
            Calls++;
            return _inner.ComputeHash(data);
        }
    }
}
