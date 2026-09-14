using Dev.Tools.Cryptography;
using Dev.Tools.Tests.Helpers;
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

        await Assert.That(provider.GetToolDefinition("base64")).IsSameReferenceAs(provider.GetToolDefinition("base-encoding"));
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

        await Assert.That(provider.GetTool("base-encoding")).IsTypeOf<BaseEncodingTool>();
        await Assert.That(provider.GetTool("base64")).IsTypeOf<BaseEncodingTool>();
        await Assert.That(provider.GetTool(typeof(BaseEncodingTool))).IsTypeOf<BaseEncodingTool>();
    }

    [Test]
    public async Task AToolResolvedByNameShouldRun()
    {
        // The point of the non-generic lookups: a host that only has a name can still run the tool.
        var tool = (ITool<BaseEncodingTool.Args, BaseEncodingTool.Result>)Provider().GetTool("base64");

        var result = await tool.RunAsync(new BaseEncodingTool.Args("hello"), CancellationToken.None);

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
        var encoder = ToolsCatalog.ToolDefinitions.Single(it => it.Name == "base-encoding");
        var clashing = encoder with { Aliases = ["hash"] };

        var exception = await Assert.That(() => new ToolsProvider(new ServiceCollection().BuildServiceProvider(), [hash, clashing]))
            .Throws<InvalidOperationException>();

        await Assert.That(exception!.Message).Contains("'hash' (hash, base-encoding)");
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

    [Test]
    public async Task ShouldInjectAHostsClockAndRandomSourceIntoTools()
    {
        var provider = Provider(services => services
            .AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)))
            .AddSingleton<IRandomSource>(new ScriptedRandomSource(0xAB)));

        var date = await provider.GetTool<DateTimeConverterTool>().RunAsync(
            new DateTimeConverterTool.Args(To: DateTimeConverterTool.DateFormatType.Unix), CancellationToken.None);
        var uuid = await provider.GetTool<UuidGeneratorTool>().RunAsync(
            new UuidGeneratorTool.Args(Type: UuidGeneratorTool.UuidType.V7), CancellationToken.None);

        await Assert.That(date.Time).IsEqualTo("1767225600");
        await Assert.That(uuid.Data.Single()).IsEqualTo("019b76da-a800-7bab-abab-abababababab");
    }

    [Test]
    public async Task ShouldDefaultToTheSystemClockAndACryptographicRandomSource()
    {
        var services = new ServiceCollection().AddDevTools().BuildServiceProvider();

        await Assert.That(services.GetRequiredService<TimeProvider>()).IsSameReferenceAs(TimeProvider.System);
        await Assert.That(services.GetRequiredService<IRandomSource>()).IsTypeOf<CryptographicRandomSource>();
    }
}
