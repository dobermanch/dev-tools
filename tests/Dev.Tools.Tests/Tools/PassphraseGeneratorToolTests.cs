using Dev.Tools.Providers;
using Dev.Tools.Tests.Helpers;
using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class PassphraseGeneratorToolTests
{
    [Test]
    [Arguments(null)]
    [Arguments(1)]
    [Arguments(2)]
    public async Task WhenPhraseCountProvided_ShouldGenerateRightAmountOfPhrases(int? expectedCount)
    {
        var args = expectedCount is null
            ? new PassphraseGeneratorTool.Args()
            : new PassphraseGeneratorTool.Args(
                PhraseCount: expectedCount.Value
            );

        var result = await new PassphraseGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Phrases.Length).IsEqualTo(expectedCount ?? args.PhraseCount);
    }
    
    // The built-in word lists contain words with spaces and hyphens ("Polar Bear", "blue-eyed"),
    // so counting words is only reliable with a separator no word can contain.
    private const char UnambiguousSeparator = '|';

    [Test]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task WhenWordCountProvided_ShouldGeneratePhraseOfRightLength(int expectedLength)
    {
        var args = new PassphraseGeneratorTool.Args(
            WordCount: expectedLength,
            Separator: UnambiguousSeparator
        );

        var result = await new PassphraseGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Phrases[0].Split(UnambiguousSeparator).Length).IsEqualTo(expectedLength);
    }

    [Test]
    public async Task WhenWordCountIsNotProvided_ShouldUseTheDefault()
    {
        var args = new PassphraseGeneratorTool.Args() with { Separator = UnambiguousSeparator };

        var result = await new PassphraseGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Phrases[0].Split(UnambiguousSeparator).Length).IsEqualTo(args.WordCount);
    }
    
    [Test]
    public async Task WhenSeparatorProvided_ShouldGeneratePhraseWithSeparator()
    {
        var args = new PassphraseGeneratorTool.Args
        (
            WordCount: 2,
            Separator: '-'
        );

        var result = await new PassphraseGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Phrases[0]).Contains(args.Separator ?? '-');
    }
    
    [Test]
    public async Task WhenSaltIsProvided_ShouldGeneratePhraseWithSalt()
    {
        var args = new PassphraseGeneratorTool.Args
        ( 
            Salt: "slat"
        );

        var result = await new PassphraseGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Phrases[0]).EndsWith(args.Salt!);
    }

    [Test]
    public async Task ShouldDrawEachWordFromTheRandomSource()
    {
        var tool = new PassphraseGeneratorTool(new ScriptedRandomSource(0, 0));

        var result = await tool.RunAsync(new PassphraseGeneratorTool.Args(WordCount: 3), CancellationToken.None);

        // An attribute, a word, then a creature - each the first of its list.
        var expected = string.Join("-", WordProvider.Attributes[0], WordProvider.Words[0], WordProvider.AllCreatures[0]).ToLower();
        await Assert.That(result.Phrases.Single()).IsEqualTo(expected);
    }
}
