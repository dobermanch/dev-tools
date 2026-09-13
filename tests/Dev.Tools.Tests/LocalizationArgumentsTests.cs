using System.Globalization;
using Dev.Tools.Localization;
using Dev.Tools.Localization.Resources;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Tests;

/// <summary>
/// A page's localizer is scoped over the composite of every translation source. Arguments given to it
/// once never reached the source, so "Changes in {0} s" showed with the placeholder in it.
/// </summary>
public class LocalizationArgumentsTests
{
    private static IStringLocalizer PageLocalizer(string key, string value)
        => new LocalizationProvider(new CurrentCultureOnly(), [new SingleTextLocalizer($"Page.Sample.{key}", value)])
            .CreateScopedLocalizer("Page.Sample");

    [Test]
    public async Task ShouldFillInTheArguments()
    {
        var localizer = PageLocalizer("Text.SecondsRemaining", "Changes in {0} s");

        await Assert.That(localizer["Text.SecondsRemaining", 5].Value).IsEqualTo("Changes in 5 s");
    }

    [Test]
    public async Task ShouldLeaveBracesAlone_WhenNoArgumentsAreGiven()
    {
        var localizer = PageLocalizer("Text.Replacement", "Refer to groups as ${name}");

        await Assert.That(localizer["Text.Replacement"].Value).IsEqualTo("Refer to groups as ${name}");
    }

    // Formats the way ResourceManagerStringLocalizer does: always, so braces without arguments throw.
    private sealed class SingleTextLocalizer(string key, string value) : IStringLocalizer
    {
        public LocalizedString this[string name]
            => name == key
                ? new LocalizedString(name, value)
                : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments]
            => name == key
                ? new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, value, arguments))
                : new LocalizedString(name, name, resourceNotFound: true);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [this[key]];
    }

    // Supports only the culture the test runs in, so the provider has no reason to switch it.
    private sealed class CurrentCultureOnly : IStringLocalizer<Locals>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => [new LocalizedString($"Culture.{CultureInfo.CurrentUICulture.Name}", string.Empty)];
    }
}
