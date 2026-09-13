using Dev.Tools.Tests.Helpers;
using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class UlidGeneratorToolTests
{
    [Test]
    [Arguments(1)]
    [Arguments(5)]
    public async Task WhenTypeIsMin_ShouldGenerateAllMinUlid(int count)
    {
        var args = new UlidGeneratorTool.Args
        (
            Type: UlidGeneratorTool.UlidType.Min,
            Count: count
        );

        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(count);

        await Assert.That(result.Data).All().Satisfy(it => it, it => it.IsEqualTo(Ulid.MinValue.ToString()));
    }

    [Test]
    [Arguments(1)]
    [Arguments(5)]
    public async Task WhenTypeIsMax_ShouldGenerateAllMaxUlid(int count)
    {
        var args = new UlidGeneratorTool.Args
        (
            Type: UlidGeneratorTool.UlidType.Max,
            Count: count
        );

        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(count);
        
        await Assert.That(result.Data).All().Satisfy(it => it, it => it.IsEqualTo(Ulid.MaxValue.ToString()));
    }

    [Test]
    [Arguments(1)]
    [Arguments(5)]
    public async Task WhenTypeIsRandom_ShouldGenerateAllRandomUlid(int count)
    {
        var args = new UlidGeneratorTool.Args
        (
            Type: UlidGeneratorTool.UlidType.Random,
            Count: count
        );

        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);
        
        await Assert.That(result.Data).Count().IsEqualTo(count);

        await Assert.That(result.Data).All().Satisfy(it => it,
            it => it.IsNotEqualTo(Ulid.MinValue.ToString()).And.IsNotEqualTo(Ulid.MaxValue.ToString()));
    }
    
    // -------------------------------
    // Formatting tests

    [Test]
    public async Task WhenCaseIsLowercase_ShouldGenerateLowercaseUlid()
    {
        var args = new UlidGeneratorTool.Args(Case: UlidGeneratorTool.UlidCase.Lowercase);
        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);
        await Assert.That(result.Data.First()).Matches("^[a-z0-9]{26}$");
    }

    [Test]
    public async Task WhenBracketsIsBraces_ShouldGenerateUlidWithBraces()
    {
        var args = new UlidGeneratorTool.Args(Brackets: UlidGeneratorTool.UlidBrackets.Braces);
        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);
        await Assert.That(result.Data.First()).Matches(@"^\{[A-Z0-9]{26}\}$");
    }

    [Test]
    public async Task WhenBracketsIsParentheses_ShouldGenerateUlidWithParentheses()
    {
        var args = new UlidGeneratorTool.Args(Brackets: UlidGeneratorTool.UlidBrackets.Parentheses);
        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);
        await Assert.That(result.Data.First()).Matches(@"^\([A-Z0-9]{26}\)$");
    }

    [Test]
    public async Task WhenBracketsIsSquareBrackets_ShouldGenerateUlidWithSquareBrackets()
    {
        var args = new UlidGeneratorTool.Args(Brackets: UlidGeneratorTool.UlidBrackets.SquareBrackets);
        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);
        await Assert.That(result.Data.First()).Matches(@"^\[[A-Z0-9]{26}\]$");
    }

    [Test]
    public async Task WhenMultipleOptions_ShouldFormatCorrectly()
    {
        var args = new UlidGeneratorTool.Args(
            Case: UlidGeneratorTool.UlidCase.Lowercase,
            Brackets: UlidGeneratorTool.UlidBrackets.Braces);
        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);
        await Assert.That(result.Data.First()).Matches(@"^\{[a-z0-9]{26}\}$");
    }

    [Test]
    public async Task WhenTypeIsRandom_ShouldBuildTheUlidFromTheClockAndTheRandomSource()
    {
        var tool = new UlidGeneratorTool(
            new ScriptedRandomSource(0xAB),
            new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));

        var result = await tool.RunAsync(new UlidGeneratorTool.Args(), CancellationToken.None);

        // "01KDVDNA00" is 1767225600000 ms, 2026-01-01T00:00:00Z; the rest is ten 0xAB bytes.
        await Assert.That(result.Data.Single()).IsEqualTo("01KDVDNA00NENTQAXBNENTQAXB");
    }
}
