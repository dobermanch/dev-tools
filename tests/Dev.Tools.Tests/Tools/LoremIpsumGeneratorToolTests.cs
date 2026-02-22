using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class LoremIpsumGeneratorToolTests
{
    [Test]
    public async Task ShouldGenerateLoremIpsum_WithDefaultParameters()
    {
        var args = new LoremIpsumGeneratorTool.Args();

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text).IsNotEmpty();
        await Assert.That(result.Text).StartsWith("Lorem ipsum dolor sit amet");
    }

    [Test]
    [Arguments(0)]
    [Arguments(-5)]
    public async Task ShouldGenerateAtLeastOneParagraph_WhenParagraphsIsZeroOrNegative(int paragraphs)
    {
        var args = new LoremIpsumGeneratorTool.Args(Paragraphs: paragraphs);

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text).IsNotEmpty();
    }

    [Test]
    [Arguments(1, 0)]
    [Arguments(3, 2)]
    [Arguments(5, 4)]
    public async Task ShouldGenerateCorrectNumberOfParagraphs(int paragraphs, int expectedSeparators)
    {
        var args = new LoremIpsumGeneratorTool.Args(Paragraphs: paragraphs);

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        var separatorCount = result.Text.Split("\n\n").Length - 1;
        await Assert.That(separatorCount).IsEqualTo(expectedSeparators);
    }

    [Test]
    [Arguments(3, 3)]
    [Arguments(5, 5)]
    public async Task ShouldGenerateCorrectNumberOfSentences(int sentences, int expectedPeriods)
    {
        var args = new LoremIpsumGeneratorTool.Args(Paragraphs: 1, SentencesPerParagraph: sentences);

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        var sentenceCount = result.Text.Count(c => c == '.');
        await Assert.That(sentenceCount).IsEqualTo(expectedPeriods);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-3)]
    public async Task ShouldGenerateAtLeastOneSentence_WhenSentencesPerParagraphIsZeroOrNegative(int sentences)
    {
        var args = new LoremIpsumGeneratorTool.Args(Paragraphs: 1, SentencesPerParagraph: sentences);

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text).Contains(".");
    }

    [Test]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(20)]
    public async Task ShouldGenerateCorrectNumberOfWords(int wordCount)
    {
        var args = new LoremIpsumGeneratorTool.Args(
            Paragraphs: 1,
            SentencesPerParagraph: 1,
            WordsPerSentence: wordCount,
            StartWithLoremIpsum: false
        );

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        var words = result.Text.TrimEnd('.').Split(' ');
        await Assert.That(words).Count().IsEqualTo(wordCount);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-5)]
    public async Task ShouldGenerateAtLeastOneWord_WhenWordsPerSentenceIsZeroOrNegative(int words)
    {
        var args = new LoremIpsumGeneratorTool.Args(
            Paragraphs: 1,
            SentencesPerParagraph: 1,
            WordsPerSentence: words,
            StartWithLoremIpsum: false
        );

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text).IsNotEmpty();
    }

    [Test]
    public async Task ShouldStartWithLoremIpsum_WhenStartWithLoremIpsumIsTrue()
    {
        var args = new LoremIpsumGeneratorTool.Args(StartWithLoremIpsum: true);

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text).StartsWith("Lorem ipsum dolor sit amet");
    }

    [Test]
    public async Task ShouldNotStartWithLoremIpsum_WhenStartWithLoremIpsumIsFalse()
    {
        var args = new LoremIpsumGeneratorTool.Args(StartWithLoremIpsum: false);

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text).DoesNotStartWith("Lorem ipsum");
    }

    [Test]
    public async Task ShouldIncludeLoremIpsumWords_WhenWordsPerSentenceIsLessThanEight()
    {
        var args = new LoremIpsumGeneratorTool.Args(
            Paragraphs: 1,
            SentencesPerParagraph: 1,
            WordsPerSentence: 5,
            StartWithLoremIpsum: true
        );

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text).IsEqualTo("Lorem ipsum dolor sit amet.");
    }

    [Test]
    public async Task ShouldExtendLoremIpsumStart_WhenWordsPerSentenceIsGreaterThanEight()
    {
        var args = new LoremIpsumGeneratorTool.Args(
            Paragraphs: 1,
            SentencesPerParagraph: 1,
            WordsPerSentence: 10,
            StartWithLoremIpsum: true
        );

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text).StartsWith("Lorem ipsum dolor sit amet consectetur adipiscing elit");
        var words = result.Text.TrimEnd('.').Split(' ');
        await Assert.That(words).Count().IsEqualTo(10);
    }

    [Test]
    public async Task ShouldCapitalizeFirstWord_InEachSentence()
    {
        var args = new LoremIpsumGeneratorTool.Args(
            Paragraphs: 1,
            SentencesPerParagraph: 3,
            StartWithLoremIpsum: false
        );

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        var sentences = result.Text.Split(". ");
        foreach (var sentence in sentences)
        {
            if (!string.IsNullOrEmpty(sentence))
            {
                await Assert.That(char.IsUpper(sentence.TrimStart()[0])).IsTrue();
            }
        }
    }

    [Test]
    public async Task ShouldOnlyStartFirstSentenceWithLoremIpsum_InMultipleParagraphs()
    {
        var args = new LoremIpsumGeneratorTool.Args(
            Paragraphs: 2,
            SentencesPerParagraph: 2,
            StartWithLoremIpsum: true
        );

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text).StartsWith("Lorem ipsum");
        var paragraphs = result.Text.Split("\n\n");
        await Assert.That(paragraphs[1]).DoesNotStartWith("Lorem ipsum");
    }

    [Test]
    public async Task ShouldGenerateLargeText_WhenLargeParametersProvided()
    {
        var args = new LoremIpsumGeneratorTool.Args(
            Paragraphs: 10,
            SentencesPerParagraph: 10,
            WordsPerSentence: 15
        );

        var result = await new LoremIpsumGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text.Length).IsGreaterThan(1000);
    }
}
