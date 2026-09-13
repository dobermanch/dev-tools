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

    // DT1004-DT1007 stop undescribed text being written; the tests below check the other half -
    // that what was written survives the generator and reaches the catalog every frontend reads.

    [Test]
    public async Task EveryToolShouldCarryATitleAndADescription()
    {
        var untitled = ToolsCatalog.ToolDefinitions
            .Where(it => string.IsNullOrWhiteSpace(it.Title) || string.IsNullOrWhiteSpace(it.Description))
            .Select(it => it.Name)
            .ToArray();

        await Assert.That(untitled).IsEmpty();
    }

    [Test]
    public async Task ToolTitlesShouldBeASingleLine()
    {
        // Titles head a CLI command list and a Web card; only Description may wrap.
        var wrapped = ToolsCatalog.ToolDefinitions
            .Where(it => it.Title.Contains('\n'))
            .Select(it => it.Name)
            .ToArray();

        await Assert.That(wrapped).IsEmpty();
    }

    [Test]
    public async Task EveryArgumentAndResultPropertyShouldCarryADescription()
    {
        var undescribed = ToolsCatalog.ToolDefinitions
            .SelectMany(tool => tool.ArgsType.Properties
                .Concat(tool.ReturnType.Properties)
                .Where(it => string.IsNullOrWhiteSpace(it.Description))
                .Select(it => $"{tool.Name}.{it.Name}"))
            .ToArray();

        await Assert.That(undescribed).IsEmpty();
    }

    [Test]
    public async Task NoPropertyShouldAuthorALabelThatTheNameAlreadyDerives()
    {
        // The convention is to author DisplayName only where the derived label misleads;
        // a DisplayName equal to the derived label is noise in the tool file.
        var redundant = ToolsCatalog.ToolDefinitions
            .SelectMany(tool => tool.ArgsType.Properties
                .Concat(tool.ReturnType.Properties)
                .Where(it => !string.IsNullOrEmpty(it.DisplayName)
                             && it.DisplayName == (it with { DisplayName = "" }).Label)
                .Select(it => $"{tool.Name}.{it.Name}"))
            .ToArray();

        await Assert.That(redundant).IsEmpty();
    }

    [Test]
    public async Task EveryEnumValueShouldCarryADescription()
    {
        var undescribed = ToolsCatalog.ToolDefinitions
            .SelectMany(tool => tool.ExtraTypes
                .OfType<ToolDefinition.EnumDetails>()
                .SelectMany(type => type.Values
                    .Where(it => string.IsNullOrWhiteSpace(it.Description))
                    .Select(it => $"{tool.Name}.{type.Name}.{it.Name}")))
            .ToArray();

        await Assert.That(undescribed).IsEmpty();
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

        // Labels that derive correctly are left unauthored, to keep the tool files readable.
        var lineBreaks = definition.ArgsType.Properties.Single(it => it.Name == "InsertLineBreaks");
        await Assert.That(lineBreaks.DisplayName).IsEmpty();
        await Assert.That(lineBreaks.Label).IsEqualTo("Insert Line Breaks");
    }

    [Test]
    [Arguments("Count", "Count")]
    [Arguments("InsertLineBreaks", "Insert Line Breaks")]
    [Arguments("UrlSafe", "Url Safe")]   // derives badly - which is why this one is authored
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

    [Test]
    public async Task ShouldListEveryErrorAToolCanReportOnce()
    {
        // The encryptor collects two errors into a list before throwing, throws a third from a switch arm
        // and a fourth directly; the catalog used to see only codes passed straight to a thrown exception.
        var codes = Definition<TextEncryptorTool>().ErrorCodes;

        await Assert.That(codes).IsEquivalentTo(
            [ErrorCode.Unknown, ErrorCode.TextEmpty, ErrorCode.InputNotValid, ErrorCode.FailedToEncrypt]);
    }
}
