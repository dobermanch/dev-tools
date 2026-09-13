using System.Globalization;
using Dev.Tools.Tests.Helpers;
using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class CronParserToolTests
{
    private static readonly DateTimeOffset Friday = new(2026, 9, 11, 12, 0, 0, TimeSpan.Zero);

    private static Task<CronParserTool.Result> RunAsync(CronParserTool.Args args, CronParserTool? tool = null)
        => (tool ?? new CronParserTool()).RunAsync(args, CancellationToken.None);

    private static string Runs(CronParserTool.Result result)
        => string.Join(" | ", result.NextRuns.Select(run => run.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture)));

    [Test]
    public async Task ShouldDescribeTheExpressionAndListItsNextRuns()
    {
        var result = await RunAsync(new CronParserTool.Args("0 9 * * MON-FRI", From: Friday, Count: 3));

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Description).IsEqualTo("At 09:00, Monday through Friday");
        await Assert.That(result.TimeZone).IsEqualTo("UTC");
        await Assert.That(Runs(result))
            .IsEqualTo("2026-09-14T09:00:00+00:00 | 2026-09-15T09:00:00+00:00 | 2026-09-16T09:00:00+00:00");
    }

    [Test]
    public async Task ShouldReadSixFieldsAsSecondsFirst()
    {
        var result = await RunAsync(new CronParserTool.Args("30 0 9 * * 1-5", From: Friday, Count: 1));

        await Assert.That(result.Description).IsEqualTo("At 09:00:30, Monday through Friday");
        await Assert.That(Runs(result)).IsEqualTo("2026-09-14T09:00:30+00:00");
    }

    [Test]
    [Arguments("@weekly", "0 0 * * 0")]
    [Arguments("@DAILY", "0 0 * * *")]
    [Arguments("@every_second", "* * * * * *")]
    [Arguments("  0\t9  * * * ", "0 9 * * *")]
    public async Task ShouldReadTheExpressionAsFieldsSeparatedBySingleSpaces(string expression, string expected)
    {
        var result = await RunAsync(new CronParserTool.Args(expression, From: Friday));

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Expression).IsEqualTo(expected);
    }

    [Test]
    public async Task ShouldRunOnADayMatchingEither_WhenBothDayFieldsAreRestricted()
    {
        var result = await RunAsync(new CronParserTool.Args("0 0 1 * MON", From: Friday, Count: 4));

        await Assert.That(result.Description).IsEqualTo("At 00:00, on day 1 of the month, or on Monday");
        await Assert.That(Runs(result))
            .IsEqualTo("2026-09-14T00:00:00+00:00 | 2026-09-21T00:00:00+00:00 | 2026-09-28T00:00:00+00:00 | 2026-10-01T00:00:00+00:00");
    }

    [Test]
    public async Task ShouldListADayMatchingBothOnce()
    {
        // 1 February 2027 is a Monday.
        var from = new DateTimeOffset(2027, 1, 30, 0, 0, 0, TimeSpan.Zero);

        var result = await RunAsync(new CronParserTool.Args("0 0 1 * MON", From: from, Count: 2));

        await Assert.That(Runs(result)).IsEqualTo("2027-02-01T00:00:00+00:00 | 2027-02-08T00:00:00+00:00");
    }

    [Test]
    public async Task ShouldRunOnADayMatchingBoth_WhenADayFieldStartsWithAStar()
    {
        var result = await RunAsync(new CronParserTool.Args("0 0 */2 * MON", From: Friday, Count: 2));

        // Odd days that are Mondays: 21 September and 5 October 2026.
        await Assert.That(Runs(result)).IsEqualTo("2026-09-21T00:00:00+00:00 | 2026-10-05T00:00:00+00:00");
    }

    [Test]
    public async Task ShouldListRunsAfterTheCurrentTime_WhenFromIsOmitted()
    {
        var tool = new CronParserTool(new FixedTimeProvider(new DateTimeOffset(2026, 9, 13, 10, 15, 0, TimeSpan.Zero)));

        var result = await RunAsync(new CronParserTool.Args("*/30 * * * *", Count: 2), tool);

        await Assert.That(Runs(result)).IsEqualTo("2026-09-13T10:30:00+00:00 | 2026-09-13T11:00:00+00:00");
    }

    [Test]
    public async Task ShouldRunAtTheEndOfTheGap_WhenClocksGoForward()
    {
        var from = new DateTimeOffset(2026, 3, 7, 12, 0, 0, TimeSpan.Zero);

        var result = await RunAsync(new CronParserTool.Args("30 2 * * *", "America/New_York", from, 2));

        await Assert.That(result.TimeZone).IsEqualTo("America/New_York");
        await Assert.That(Runs(result)).IsEqualTo("2026-03-08T03:00:00-04:00 | 2026-03-09T02:30:00-04:00");
    }

    [Test]
    public async Task ShouldRunAFixedTimeOnce_WhenClocksGoBack()
    {
        var from = new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

        var result = await RunAsync(new CronParserTool.Args("30 1 * * *", "America/New_York", from, 2));

        await Assert.That(Runs(result)).IsEqualTo("2026-11-01T01:30:00-04:00 | 2026-11-02T01:30:00-05:00");
    }

    [Test]
    public async Task ShouldRunARepeatingScheduleThroughBothPasses_WhenClocksGoBack()
    {
        var from = new DateTimeOffset(2026, 11, 1, 4, 30, 0, TimeSpan.Zero);

        var result = await RunAsync(new CronParserTool.Args("0 * * * *", "America/New_York", from, 3));

        await Assert.That(Runs(result))
            .IsEqualTo("2026-11-01T01:00:00-04:00 | 2026-11-01T01:00:00-05:00 | 2026-11-01T02:00:00-05:00");
    }

    [Test]
    public async Task ShouldDescribeButListNoRuns_WhenTheExpressionNeverMatches()
    {
        var result = await RunAsync(new CronParserTool.Args("0 0 30 2 *", From: Friday));

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Description).IsEqualTo("At 00:00, on day 30 of the month, only in February");
        await Assert.That(result.NextRuns).IsEmpty();
    }

    [Test]
    [Arguments(0, 1)]
    [Arguments(500, 100)]
    public async Task ShouldClampTheCount(int count, int expected)
    {
        var result = await RunAsync(new CronParserTool.Args("* * * * *", From: Friday, Count: count));

        await Assert.That(result.NextRuns.Count).IsEqualTo(expected);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task ShouldReturnError_WhenTheExpressionIsEmpty(string? expression)
    {
        var result = await RunAsync(new CronParserTool.Args(expression));

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.TextEmpty);
        await Assert.That(result.Errors[0].Target).IsEqualTo(nameof(CronParserTool.Args.Expression));
    }

    [Test]
    [Arguments("bad")]
    [Arguments("60 * * * *")]
    [Arguments("* * * *")]
    [Arguments("0 0 0 1 1 ? 2030")]
    [Arguments("H 0 * * *")]
    [Arguments("@fortnightly")]
    public async Task ShouldReturnError_WhenTheExpressionIsNotValid(string expression)
    {
        var result = await RunAsync(new CronParserTool.Args(expression));

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.InputNotValid);
        await Assert.That(result.Errors[0].Target).IsEqualTo(nameof(CronParserTool.Args.Expression));
    }

    [Test]
    public async Task ShouldReportEveryInvalidArgument()
    {
        var result = await RunAsync(new CronParserTool.Args("60 * * * *", "Mars/Olympus_Mons"));

        await Assert.That(result.Errors.Select(it => $"{it.Code} {it.Target}"))
            .IsEquivalentTo(["InputNotValid Expression", "InputNotValid TimeZone"]);
    }
}
