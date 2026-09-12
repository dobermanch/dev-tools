using Dev.Tools.Tools;

namespace Dev.Tools.Tests;

public class ToolsCatalogTests
{
    private static ToolDefinition Definition<T>()
        => ToolsCatalog.ToolDefinitions.Single(it => it.ToolType == typeof(T));

    [Test]
    public async Task ShouldExposeEnglishTextFromTheToolDefinition()
    {
        var definition = Definition<Base64EncoderTool>();

        await Assert.That(definition.Title).IsEqualTo("Base64 Encoder");
        await Assert.That(definition.Description).StartsWith("Encodes text to Base64 format");
    }

    [Test]
    public async Task ShouldExposePropertyDescriptions()
    {
        var definition = Definition<Base64EncoderTool>();

        var text = definition.ArgsType.Properties.Single(it => it.Name == "Text");
        var encoded = definition.ReturnType.Properties.Single(it => it.Name == "Text");

        await Assert.That(text.Description).IsEqualTo("Text to encode to Base64 format");
        await Assert.That(encoded.Description).IsEqualTo("The Base64-encoded text");
    }

    [Test]
    public async Task ShouldFallBackToToolNameWhenTitleIsNotSet()
    {
        // Until every tool carries text, an unset Title must still give callers something to show.
        await Assert.That(ToolsCatalog.ToolDefinitions.All(it => !string.IsNullOrWhiteSpace(it.Title))).IsTrue();
    }

    [Test]
    [Arguments("One line only", "One line only")]
    [Arguments("Summary line\n\nDetail paragraph that callers with room can show.", "Summary line")]
    [Arguments("Trailing spaces kept out   \nmore", "Trailing spaces kept out")]
    [Arguments("", "")]
    public async Task SummaryShouldBeTheFirstLineOfADetailedDescription(string description, string expected)
    {
        var definition = Definition<Base64EncoderTool>() with { Description = description };

        await Assert.That(definition.Summary).IsEqualTo(expected);
    }

    [Test]
    public async Task ShouldExposeAuthoredPropertyLabels()
    {
        var definition = Definition<Base64EncoderTool>();

        // "Text" on the result means "Encoded Text" to a reader - that is what DisplayName is for.
        var encoded = definition.ReturnType.Properties.Single(it => it.Name == "Text");

        await Assert.That(encoded.DisplayName).IsEqualTo("Encoded Text");
        await Assert.That(encoded.Label).IsEqualTo("Encoded Text");
    }

    [Test]
    [Arguments("Count", "Count")]
    [Arguments("InsertLineBreaks", "Insert Line Breaks")]
    [Arguments("UrlSafe", "Url Safe")]
    [Arguments("IPv4", "IPv4")]
    public async Task LabelShouldFallBackToTheSplitPropertyNameWhenNoDisplayNameIsAuthored(string propertyName, string expected)
    {
        var property = new ToolDefinition.TypeProperty(propertyName, typeof(string), false, false);

        await Assert.That(property.Label).IsEqualTo(expected);
    }

    [Test]
    public async Task ShouldExposeEnumValueNames()
    {
        var definition = Definition<UuidGeneratorTool>();

        var uuidType = definition.ExtraTypes
            .OfType<ToolDefinition.EnumDetails>()
            .Single(it => it.Name == nameof(UuidGeneratorTool.UuidType));

        await Assert.That(uuidType.Values.Select(it => it.Name)).Contains("V7");
    }
}
