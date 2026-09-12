using System.Globalization;
using System.Resources;
using Dev.Tools.Localization;
using Dev.Tools.Localization.Resources;
using Dev.Tools.Providers;
using Dev.Tools.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dev.Tools.Tests;

/// <summary>
/// English is authored in the tool sources and reaches the Web frontend through the catalog
/// localizer; the RESX files hold translations of it. These tests hold both halves of that.
/// </summary>
public class LocalizationTests
{
    private static IStringLocalizer CatalogLocalizer()
    {
        var services = new ServiceCollection();
        services.AddDevTools();

        return new ToolCatalogStringLocalizer(
            services.BuildServiceProvider().GetRequiredService<IToolsProvider>());
    }

    private static ILocalizationProvider Provider()
    {
        var services = new ServiceCollection();
        // ResourceManagerStringLocalizerFactory asks for one; a host always has it.
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddDevTools();
        services.AddDevToolsLocalization();

        return services.BuildServiceProvider().GetRequiredService<ILocalizationProvider>();
    }

    [Test]
    public async Task ShouldServeToolTitlesAndDescriptionsFromTheToolSources()
    {
        var localizer = CatalogLocalizer();

        await Assert.That(localizer["Tools.Base64Encoder.Name"].Value).IsEqualTo("Base64 Encoder");
        await Assert.That(localizer["Tools.Base64Encoder.Description"].Value)
            .StartsWith("Encodes text to Base64 format");
    }

    [Test]
    public async Task ShouldServeArgumentAndResultTextFromTheToolSources()
    {
        var localizer = CatalogLocalizer();

        await Assert.That(localizer["Tools.Base64Encoder.Args.UrlSafe.Name"].Value).IsEqualTo("URL Safe");
        await Assert.That(localizer["Tools.Base64Encoder.Args.InsertLineBreaks.Name"].Value)
            .IsEqualTo("Insert Line Breaks");
        await Assert.That(localizer["Tools.Base64Encoder.Result.Text.Name"].Value).IsEqualTo("Encoded Text");
        await Assert.That(localizer["Tools.Base64Encoder.Result.Text.Description"].Value)
            .IsEqualTo("The Base64-encoded text");
    }

    [Test]
    public async Task ShouldServeEnumValueTextFromTheToolSources()
    {
        var localizer = CatalogLocalizer();

        await Assert.That(localizer["Tools.UuidGenerator.Enums.UuidType.V4"].Value)
            .IsEqualTo("Version 4, random");

        // Category and Keyword only carry text where the label differs from the member name.
        await Assert.That(localizer["Enums.Category.Crypto"].Value).IsEqualTo("Cryptography");
        await Assert.That(localizer["Enums.Category.Network"].Value).IsEqualTo("Network");
        await Assert.That(localizer["Enums.Keyword.Ip"].Value).IsEqualTo("IP Address");
        await Assert.That(localizer["Enums.ErrorCode.TextEmpty"].Value).IsEqualTo("Text cannot be empty");
    }

    [Test]
    public async Task ShouldServeEveryKeyTheWebFrontendAsksForWithoutAnyResourceFile()
    {
        var localizer = CatalogLocalizer();
        var tools = new ServiceCollection().AddDevTools().BuildServiceProvider()
            .GetRequiredService<IToolsProvider>();

        var missing = tools.GetToolDefinitions()
            .SelectMany(tool => new[]
            {
                localizer.GetToolTitle(tool.ToolType.Name),
                localizer.GetToolDescription(tool.ToolType.Name),
            })
            .Where(string.IsNullOrWhiteSpace)
            .ToArray();

        await Assert.That(missing).IsEmpty();
    }

    [Test]
    public async Task AMissingKeyShouldFallBackToEnglishRatherThanAPlaceholder()
    {
        // Adding a tool no longer involves touching a RESX file, so its keys exist nowhere else.
        var provider = Provider();

        var title = provider["Tools.Base64Encoder.Name"].Value;

        await Assert.That(title).IsEqualTo("Base64 Encoder");
        await Assert.That(title).DoesNotStartWith(":");
    }

    [Test]
    public async Task ATranslationShouldWinOverTheEnglishInTheToolSources()
    {
        // The catalog localizer is registered last precisely so this ordering holds.
        var provider = Provider();
        var previous = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("uk-UA");

            await Assert.That(provider["Tools.Base64Encoder.Name"].Value).IsEqualTo("Кодер Base64");
            await Assert.That(provider["Enums.Category.Crypto"].Value).IsEqualTo("Криптографія");
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Test]
    public async Task EveryTranslatedCultureShouldBeComplete()
    {
        var english = EnglishKeys();

        var report = TranslatedCultures()
            .SelectMany(culture => english
                .Except(TranslatedKeys(culture), StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .Select(key => $"  {culture}  {key}\n    dotnet run --project tools/Translations -- {culture} --write"))
            .ToArray();

        await Assert.That(string.Join("\n", report)).IsEmpty();
    }

    [Test]
    public async Task NoTranslatedCultureShouldCarryKeysNothingAsksFor()
    {
        var english = EnglishKeys();

        var report = TranslatedCultures()
            .SelectMany(culture => TranslatedKeys(culture)
                .Except(english, StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .Select(key => $"  {culture}  {key}\n    dotnet run --project tools/Translations -- {culture} --write"))
            .ToArray();

        await Assert.That(string.Join("\n", report)).IsEmpty();
    }

    [Test]
    public async Task ShouldBeCheckingAtLeastOneTranslation()
    {
        // The two tests above pass vacuously if no satellite is found, which would quietly
        // stop translations being checked at all.
        await Assert.That(TranslatedCultures()).IsNotEmpty();
    }

    private static IReadOnlyCollection<string> EnglishKeys()
        => CatalogLocalizer().GetAllStrings(false).Select(it => it.Name).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Every culture whose satellite assembly carries a ToolResources set, so a newly added
    /// translation is checked the moment it exists without this test naming cultures. Cultures with
    /// only other resources - "en" has Locals but no ToolResources - are not translations of the
    /// tool text and are skipped.
    /// </summary>
    private static IEnumerable<CultureInfo> TranslatedCultures()
    {
        foreach (var directory in new DirectoryInfo(AppContext.BaseDirectory).EnumerateDirectories())
        {
            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(directory.Name);
            }
            catch (CultureNotFoundException)
            {
                continue;
            }

            using var set = ToolResourceSet(culture);
            if (set is not null)
            {
                yield return culture;
            }
        }
    }

    private static IReadOnlyCollection<string> TranslatedKeys(CultureInfo culture)
    {
        using var set = ToolResourceSet(culture);

        return set is null
            ? []
            : set.Cast<System.Collections.DictionaryEntry>()
                .Select(it => (string)it.Key)
                .ToHashSet(StringComparer.Ordinal);
    }

    private static ResourceSet? ToolResourceSet(CultureInfo culture)
        => new ResourceManager("Dev.Tools.Localization.Resources.ToolResources", typeof(ToolResources).Assembly)
            .GetResourceSet(culture, createIfNotExists: true, tryParents: false);
}
