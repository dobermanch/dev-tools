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
            : new PassphraseGeneratorTool.Args
            {
                PhraseCount = expectedCount.Value
            };

        var result = await new PassphraseGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Phrases.Length).IsEqualTo(expectedCount ?? args.PhraseCount);
    }
    
    [Test]
    [Arguments(null)]
    [Arguments(1)]
    [Arguments(10)]
    public async Task WhenWordCountProvided_ShouldGeneratePhraseOfRightLength(int? expectedLength)
    {
        var args = expectedLength is null
            ? new PassphraseGeneratorTool.Args()
            : new PassphraseGeneratorTool.Args
            {
                WordCount = expectedLength.Value,
                Separator = ' '
            };

        var result = await new PassphraseGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Phrases[0].Split(args.Separator ?? ' ').Length).IsEqualTo(expectedLength ?? args.WordCount);
    }
    
    [Test]
    public async Task WhenSeparatorProvided_ShouldGeneratePhraseWithSeparator()
    {
        var args = new PassphraseGeneratorTool.Args
        {
            WordCount = 2,
            Separator = '-'
        };

        var result = await new PassphraseGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Phrases[0]).Contains(args.Separator ?? '-');
    }
    
    [Test]
    public async Task WhenSaltIsProvided_ShouldGeneratePhraseWithSalt()
    {
        var args = new PassphraseGeneratorTool.Args
        { 
            Salt = "slat"
        };

        var result = await new PassphraseGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Phrases[0]).EndsWith(args.Salt!);
    }
}
