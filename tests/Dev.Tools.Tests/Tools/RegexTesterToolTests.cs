using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class RegexTesterToolTests
{
    private static Task<RegexTesterTool.Result> RunAsync(RegexTesterTool.Args args, RegexTesterTool? tool = null)
        => (tool ?? new RegexTesterTool()).RunAsync(args, CancellationToken.None);

    // "index line:column value [group=index:value, group=-]", so a failing test shows the whole match.
    private static string Describe(RegexTesterTool.MatchInfo match)
        => $"{match.Index} {match.Line}:{match.Column} {match.Value} ["
           + string.Join(", ", match.Groups.Select(group => group.Success ? $"{group.Name}={group.Index}:{group.Value}" : $"{group.Name}=-"))
           + "]";

    [Test]
    public async Task ShouldListEveryMatchWithItsPositionAndGroups()
    {
        var result = await RunAsync(new RegexTesterTool.Args("id=12\nx id=345", @"id=(?<num>\d+)"));

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.MatchCount).IsEqualTo(2);
        await Assert.That(result.Output).IsEqualTo("id=12\nid=345");
        await Assert.That(string.Join(" | ", result.Matches.Select(Describe)))
            .IsEqualTo("0 1:1 id=12 [num=3:12] | 8 2:3 id=345 [num=11:345]");
    }

    [Test]
    public async Task ShouldReportAGroupThatTookNoPartInTheMatch()
    {
        var result = await RunAsync(new RegexTesterTool.Args("b", "(a)|(b)"));

        await Assert.That(Describe(result.Matches[0])).IsEqualTo("0 1:1 b [1=-, 2=0:b]");
    }

    [Test]
    public async Task ShouldCountLinesAcrossWindowsLineBreaks()
    {
        var result = await RunAsync(new RegexTesterTool.Args("a\r\n  b", "b"));

        await Assert.That(Describe(result.Matches[0])).IsEqualTo("5 2:3 b []");
    }

    [Test]
    public async Task ShouldReturnNoMatches_WhenThePatternDoesNotMatch()
    {
        var result = await RunAsync(new RegexTesterTool.Args("hello", @"\d"));

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.MatchCount).IsEqualTo(0);
        await Assert.That(result.Matches).IsEmpty();
        await Assert.That(result.Output).IsEqualTo(string.Empty);
    }

    [Test]
    [Arguments("2024-01-15", @"(?<y>\d{4})-(?<m>\d\d)-(?<d>\d\d)", "${d}.${m}.$1", "15.01.2024")]
    [Arguments("a1b2", @"\d", "", "ab")]
    [Arguments("hello", @"\d", "#", "hello")]
    public async Task ShouldReplaceEveryMatch_WhenGivenAReplacement(string text, string pattern, string replacement, string expected)
    {
        var result = await RunAsync(new RegexTesterTool.Args(text, pattern, replacement));

        await Assert.That(result.Output).IsEqualTo(expected);
    }

    [Test]
    [Arguments("ABC", "abc", false, false, false, false, 0)]
    [Arguments("ABC", "abc", true, false, false, false, 1)]
    [Arguments("a\nb", "^b$", false, false, false, false, 0)]
    [Arguments("a\nb", "^b$", false, true, false, false, 1)]
    [Arguments("a\nb", "a.b", false, false, false, false, 0)]
    [Arguments("a\nb", "a.b", false, false, true, false, 1)]
    [Arguments("ab", "a b # a comment", false, false, false, false, 0)]
    [Arguments("ab", "a b # a comment", false, false, false, true, 1)]
    public async Task ShouldApplyEachOption(
        string text, string pattern, bool ignoreCase, bool multiline, bool singleline, bool ignorePatternWhitespace, int expected)
    {
        var result = await RunAsync(new RegexTesterTool.Args(
            text, pattern, IgnoreCase: ignoreCase, Multiline: multiline, Singleline: singleline, IgnorePatternWhitespace: ignorePatternWhitespace));

        await Assert.That(result.MatchCount).IsEqualTo(expected);
    }

    [Test]
    public async Task ShouldCountEveryMatch_AndListTheFirstThousand()
    {
        var result = await RunAsync(new RegexTesterTool.Args(new string('a', 1500), "a"));

        await Assert.That(result.MatchCount).IsEqualTo(1500);
        await Assert.That(result.Matches.Count).IsEqualTo(1000);
        await Assert.That(result.Output.Split('\n').Length).IsEqualTo(1500);
    }

    [Test]
    [Arguments("(")]
    [Arguments("[a-")]
    [Arguments(@"\k<missing>")]
    public async Task ShouldReturnInputNotValid_WhenThePatternIsNotAValidRegex(string pattern)
    {
        var result = await RunAsync(new RegexTesterTool.Args("text", pattern));

        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.InputNotValid);
        await Assert.That(result.Errors[0].Target).IsEqualTo(nameof(RegexTesterTool.Args.Pattern));
    }

    [Test]
    public async Task ShouldReportBothMissingInputs()
    {
        var result = await RunAsync(new RegexTesterTool.Args(null, null!));

        await Assert.That(result.Errors.Select(it => $"{it.Code}:{it.Target}").ToArray())
            .IsEquivalentTo(new[] { "TextEmpty:Text", "TextEmpty:Pattern" });
    }

    [Test]
    public async Task ShouldReturnMatchTimeout_WhenThePatternBacktracksCatastrophically()
    {
        // Two overlapping branches and a mismatch at the very end: the engine tries every way of
        // splitting the a's before giving up, which doubles with each one.
        var tool = new RegexTesterTool(TimeSpan.FromMilliseconds(20));

        var result = await RunAsync(new RegexTesterTool.Args(new string('a', 60) + "b", "^(a|aa)+$"), tool);

        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.MatchTimeout);
        await Assert.That(result.Errors[0].Target).IsEqualTo(nameof(RegexTesterTool.Args.Pattern));
    }
}
