using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

/// <summary>
/// The expected diffs are what GNU diff -u --label original --label modified writes for the same texts. The tool
/// leaves out the final line break, which the CLI adds when it prints, so the tests put it back to compare.
/// </summary>
public class TextDiffToolTests
{
    private const string Header = "--- original\n+++ modified\n";

    private static Task<TextDiffTool.Result> RunAsync(string? original, string? modified, bool ignoreWhitespace = false, bool ignoreCase = false, int contextLines = 3)
        => new TextDiffTool().RunAsync(new TextDiffTool.Args(modified, original, ignoreWhitespace, ignoreCase, contextLines), CancellationToken.None);

    [Test]
    public async Task ShouldShowChangesCloseTogetherInOneHunk()
    {
        // Six unchanged lines between the changes: exactly twice the context, so they share a hunk.
        var result = await RunAsync("a\nb\nc\nd\ne\nf\ng\nh\n", "a\nB\nc\nd\ne\nf\ng\nh\ni\n");

        await Assert.That(result.Diff + "\n").IsEqualTo(Header + "@@ -1,8 +1,9 @@\n a\n-b\n+B\n c\n d\n e\n f\n g\n h\n+i\n");
        await Assert.That(result.HasChanges).IsTrue();
        await Assert.That(result.AddedLines).IsEqualTo(2);
        await Assert.That(result.RemovedLines).IsEqualTo(1);
    }

    [Test]
    public async Task ShouldSplitChangesFurtherApartIntoHunks()
    {
        var result = await RunAsync("1\n2\n3\n4\n5\n6\n7\n8\n9\n10\n11\n", "1\nX\n3\n4\n5\n6\n7\n8\n9\n10\nY\n");

        await Assert.That(result.Diff + "\n").IsEqualTo(Header
            + "@@ -1,5 +1,5 @@\n 1\n-2\n+X\n 3\n 4\n 5\n"
            + "@@ -8,4 +8,4 @@\n 8\n 9\n 10\n-11\n+Y\n");
    }

    [Test]
    [Arguments("a\nb", "a\nc", "@@ -1,2 +1,2 @@\n a\n-b\n\\ No newline at end of file\n+c\n\\ No newline at end of file\n")]
    [Arguments("a", "a\n", "@@ -1 +1 @@\n-a\n\\ No newline at end of file\n+a\n")]
    [Arguments("x\na", "y\na", "@@ -1,2 +1,2 @@\n-x\n+y\n a\n\\ No newline at end of file\n")]
    public async Task ShouldMarkALastLineWithoutALineBreak(string original, string modified, string hunks)
    {
        var result = await RunAsync(original, modified);

        await Assert.That(result.Diff + "\n").IsEqualTo(Header + hunks);
    }

    [Test]
    [Arguments("", "new\n", "@@ -0,0 +1 @@\n+new\n")]
    [Arguments("old\n", "", "@@ -1 +0,0 @@\n-old\n")]
    [Arguments(null, "new\n", "@@ -0,0 +1 @@\n+new\n")]
    [Arguments("x\na\n", "a\n", "@@ -1,2 +1 @@\n-x\n a\n")]
    public async Task ShouldNumberAHunkThatHoldsNoLinesOfOneText(string? original, string modified, string hunks)
    {
        var result = await RunAsync(original, modified);

        await Assert.That(result.Diff + "\n").IsEqualTo(Header + hunks);
    }

    [Test]
    public async Task ShouldShowOnlyTheChange_WhenContextIsZero()
    {
        var result = await RunAsync("1\n2\n3\n4\n5\n", "1\n2\nX\n4\n5\n", contextLines: 0);

        await Assert.That(result.Diff + "\n").IsEqualTo(Header + "@@ -3 +3 @@\n-3\n+X\n");
    }

    [Test]
    public async Task ShouldTreatNegativeContextAsZero()
    {
        var result = await RunAsync("1\n2\n3\n", "1\nX\n3\n", contextLines: -5);

        await Assert.That(result.Diff + "\n").IsEqualTo(Header + "@@ -2 +2 @@\n-2\n+X\n");
    }

    [Test]
    [Arguments("a\nb\n", "a\nb\n")]
    [Arguments("a\r\nb\r\n", "a\nb\n")]
    [Arguments("a\rb\r", "a\nb\n")]
    [Arguments("", "")]
    [Arguments(null, null)]
    public async Task ShouldFindNoChanges_WhenTheTextsMatch(string? original, string? modified)
    {
        var result = await RunAsync(original, modified);

        await Assert.That(result.Diff).IsEmpty();
        await Assert.That(result.HasChanges).IsFalse();
        await Assert.That(result.AddedLines).IsEqualTo(0);
        await Assert.That(result.RemovedLines).IsEqualTo(0);
    }

    [Test]
    [Arguments("a b\n", "a  b \n", true, false)]
    [Arguments("ab\n", "a\tb\n", true, false)]
    [Arguments("Hello\n", "hELLO\n", false, true)]
    [Arguments("Hello World\n", "hello  world\n", true, true)]
    public async Task ShouldFindNoChanges_WhenOnlyWhatIsIgnoredDiffers(string original, string modified, bool ignoreWhitespace, bool ignoreCase)
    {
        var compared = await RunAsync(original, modified);
        var ignored = await RunAsync(original, modified, ignoreWhitespace, ignoreCase);

        await Assert.That(compared.HasChanges).IsTrue();
        await Assert.That(ignored.HasChanges).IsFalse();
    }

    [Test]
    public async Task ShouldShowTheLinesAsWritten_WhenIgnoringWhitespace()
    {
        var result = await RunAsync("Keep  this\nold\n", "Keep this\nnew\n", ignoreWhitespace: true);

        await Assert.That(result.Diff + "\n").IsEqualTo(Header + "@@ -1,2 +1,2 @@\n Keep  this\n-old\n+new\n");
    }

    [Test]
    public async Task ShouldListEveryLineWithItsNumberInEachText()
    {
        var result = await RunAsync("a\nb\nc\n", "a\nB\nc\nd\n");

        var lines = result.Lines.Select(it => $"{it.Kind} {it.OriginalLine?.ToString() ?? "-"} {it.ModifiedLine?.ToString() ?? "-"} {it.Text}");
        await Assert.That(string.Join(" | ", lines))
            .IsEqualTo("Unchanged 1 1 a | Removed 2 - b | Added - 2 B | Unchanged 3 3 c | Added - 4 d");
    }
}
