using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class PassphraseGeneratorToolTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task WhenPhraseCountProvided_ShouldGenerateRightAmountOfPhrases(int? expectedCount)
    {
        var args = expectedCount is null
            ? new PassphraseGeneratorTool.Args()
            : new PassphraseGeneratorTool.Args
            {
                PhraseCount = expectedCount.Value
            };

        var result = await new PassphraseGeneratorTool().RunAsync(args, CancellationToken.None);

        Assert.Equal(expectedCount ?? args.PhraseCount, result.Phrases.Length);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(10)]
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

        Assert.Equal(expectedLength ?? args.WordCount, result.Phrases[0].Split(args.Separator ?? ' ').Length);
    }
    
    [Fact]
    public async Task WhenSeparatorProvided_ShouldGeneratePhraseWithSeparator()
    {
        var args = new PassphraseGeneratorTool.Args
        {
            WordCount = 2,
            Separator = '-'
        };

        var result = await new PassphraseGeneratorTool().RunAsync(args, CancellationToken.None);

        Assert.Contains(args.Separator ?? '-', result.Phrases[0]);
    }
    
    [Fact]
    public async Task WhenSaltIsProvided_ShouldGeneratePhraseWithSalt()
    {
        var args = new PassphraseGeneratorTool.Args
        { 
            Salt = "slat"
        };

        var result = await new PassphraseGeneratorTool().RunAsync(args, CancellationToken.None);

        Assert.EndsWith(args.Salt!, result.Phrases[0]);
    }
}
