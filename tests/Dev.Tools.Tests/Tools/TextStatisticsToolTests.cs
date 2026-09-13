using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class TextStatisticsToolTests
{
    private static Task<TextStatisticsTool.Result> RunAsync(string? text)
        => new TextStatisticsTool().RunAsync(new TextStatisticsTool.Args(text), CancellationToken.None);

    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task ShouldCountZeroOfEverything_WhenTextIsEmpty(string? text)
    {
        var result = await RunAsync(text);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result).IsEqualTo(new TextStatisticsTool.Result());
    }

    [Test]
    public async Task ShouldCountPlainText()
    {
        var result = await RunAsync("Hello, world!");

        await Assert.That(result).IsEqualTo(new TextStatisticsTool.Result(
            Characters: 13,
            NonWhitespaceCharacters: 12,
            Words: 2,
            Lines: 1,
            Paragraphs: 1,
            Utf8Bytes: 13,
            Utf16Length: 13));
    }

    [Test]
    public async Task ShouldCountWhatAReaderSees_WhenCharactersTakeSeveralCodeUnits()
    {
        // A thumbs-up with a skin tone (two code points, four UTF-16 units, eight UTF-8 bytes) and
        // "café" spelled with a combining acute accent.
        var result = await RunAsync("\U0001F44D\U0001F3FD café");

        await Assert.That(result.Characters).IsEqualTo(6);
        await Assert.That(result.NonWhitespaceCharacters).IsEqualTo(5);
        await Assert.That(result.Utf16Length).IsEqualTo(10);
        await Assert.That(result.Utf8Bytes).IsEqualTo(15);
        // The emoji holds no letter or digit, so it is not a word.
        await Assert.That(result.Words).IsEqualTo(1);
    }

    [Test]
    public async Task ShouldCountBytesOfNonLatinText()
    {
        var result = await RunAsync("Привіт");

        await Assert.That(result.Characters).IsEqualTo(6);
        await Assert.That(result.Utf8Bytes).IsEqualTo(12);
    }

    [Test]
    [Arguments("one two three", 3)]
    [Arguments("  one\ttwo\nthree  ", 3)]
    [Arguments("a - b", 2)]
    [Arguments("- — ...", 0)]
    [Arguments("don't stop-motion", 2)]
    [Arguments("Привіт світ", 2)]
    [Arguments("日本語のテキスト", 1)]
    [Arguments("version 2", 2)]
    public async Task ShouldCountWords(string text, int expected)
    {
        var result = await RunAsync(text);

        await Assert.That(result.Words).IsEqualTo(expected);
    }

    [Test]
    [Arguments("a", 1)]
    [Arguments("a\n", 2)]
    [Arguments("\n", 2)]
    [Arguments("a\r\nb\rc\nd", 4)]
    [Arguments("a\u2028b", 2)]
    [Arguments("   ", 1)]
    public async Task ShouldCountLines(string text, int expected)
    {
        var result = await RunAsync(text);

        await Assert.That(result.Lines).IsEqualTo(expected);
    }

    [Test]
    [Arguments("one\ntwo", 1)]
    [Arguments("one\ntwo\n\n  \nthree\n", 2)]
    [Arguments("\r\n\r\nonly\r\n\r\n", 1)]
    [Arguments("   ", 0)]
    public async Task ShouldCountParagraphs(string text, int expected)
    {
        var result = await RunAsync(text);

        await Assert.That(result.Paragraphs).IsEqualTo(expected);
    }
}
