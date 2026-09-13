using CronExpressionDescriptor;
using Cronos;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "cron-parser",
    Title = "Cron Expression Parser",
    Description =
        """
        Explains a cron expression in English and lists the next times it runs.

        The expression has five fields - minute, hour, day of month, month, day of week - or six with
        seconds in front. A field takes *, lists (1,15), ranges (1-5), steps (*/10), month and day names
        (JAN, MON), and ? in place of *; day of week 0 and 7 are both Sunday. L, W and # pick the last day,
        the nearest weekday and the nth weekday of the month: 5#3 is the third Friday. The macros @yearly,
        @annually, @monthly, @weekly, @daily, @midnight, @hourly, @every_minute and @every_second stand for
        the matching expression. There is no year field, and H is not supported. As in crontab, when both day
        of month and day of week are restricted a day matching either one runs, so 0 0 1 * MON runs on the
        1st and on every Monday; a field starting with * does not count as restricted.

        Run times are computed in the given time zone, UTC when none is given. When clocks go forward, a
        time in the skipped hour runs at the end of the gap; when they go back, a fixed time of day runs
        once, while an expression that repeats within the day, such as every 30 minutes, runs through both
        passes. An expression that can never match, such as 30 February, is described but has no run times.
        """,
    Usage =
        "Use this rather than reading a cron expression by eye or working out its run times by hand: day-of-month and day-of-week combinations, L, W and #, and daylight saving changes are easy to get wrong. It reads crontab syntax with an optional leading seconds field - not Quartz's trailing year field, and not H. Day of month and day of week combine as crontab combines them (either one matching), not as Quartz or Hangfire do. Pass the TimeZone the scheduler runs in, since the run times depend on it.",
    Aliases = ["cron"],
    Keywords = [Keyword.Cron],
    Categories = [Category.Development]
)]
public sealed class CronParserTool(TimeProvider? time = null)
    : ToolBase<CronParserTool.Args, CronParserTool.Result>
{
    private const int MaxCount = 100;

    // Cronos reads the macros itself but the descriptor does not, so both are given the expression a macro stands for.
    private static readonly Dictionary<string, string> Macros = new(StringComparer.OrdinalIgnoreCase)
    {
        ["@yearly"] = "0 0 1 1 *",
        ["@annually"] = "0 0 1 1 *",
        ["@monthly"] = "0 0 1 * *",
        ["@weekly"] = "0 0 * * 0",
        ["@daily"] = "0 0 * * *",
        ["@midnight"] = "0 0 * * *",
        ["@hourly"] = "0 * * * *",
        ["@every_minute"] = "* * * * *",
        ["@every_second"] = "* * * * * *"
    };

    private readonly TimeProvider _time = time ?? TimeProvider.System;

    protected override Result Execute(Args args)
    {
        var errors = new List<ToolError>();

        string? expression = null;
        CronExpression[]? schedule = null;
        if (string.IsNullOrWhiteSpace(args.Expression))
        {
            errors.Add(new ToolError(ErrorCode.TextEmpty, nameof(Args.Expression)));
        }
        else
        {
            expression = Normalize(args.Expression);
            schedule = Parse(expression);
            if (schedule is null)
            {
                errors.Add(new ToolError(ErrorCode.InputNotValid, nameof(Args.Expression)));
            }
        }

        var zone = FindTimeZone(args.TimeZone);
        if (zone is null)
        {
            errors.Add(new ToolError(ErrorCode.InputNotValid, nameof(Args.TimeZone)));
        }

        if (errors.Count > 0)
        {
            throw new ToolException(errors);
        }

        return new Result(Describe(expression!), expression!, zone!.Id, NextRuns(schedule!, zone, args));
    }

    // One space between fields, and a macro written out, so both libraries read the same expression.
    private static string Normalize(string expression)
    {
        var fields = expression.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length == 1 && Macros.TryGetValue(fields[0], out var expanded))
        {
            return expanded;
        }

        return string.Join(' ', fields);
    }

    // crontab runs on a day matching either the day of month or the day of week when both are restricted, while
    // Cronos requires both - so the expression is then parsed once per day field, and the run times merged.
    private static CronExpression[]? Parse(string expression)
    {
        var fields = expression.Split(' ');
        var format = fields.Length == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard;
        var dayOfMonth = fields.Length - 3;
        var dayOfWeek = fields.Length - 1;
        try
        {
            if (fields.Length is 5 or 6 && IsRestricted(fields[dayOfMonth]) && IsRestricted(fields[dayOfWeek]))
            {
                return
                [
                    CronExpression.Parse(Replace(fields, dayOfWeek, "*"), format),
                    CronExpression.Parse(Replace(fields, dayOfMonth, "*"), format)
                ];
            }

            return [CronExpression.Parse(expression, format)];
        }
        catch (CronFormatException)
        {
            return null;
        }
        catch (MissingSeedException)
        {
            // H, which needs a seed the expression does not carry.
            return null;
        }
    }

    // As cronie decides it: a field starting with * is unrestricted, */2 included.
    private static bool IsRestricted(string field) => !field.StartsWith('*') && field != "?";

    private static string Replace(string[] fields, int index, string value)
        => string.Join(' ', fields.Select((field, i) => i == index ? value : field));

    private static TimeZoneInfo? FindTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id.Trim());
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return null;
        }
    }

    // Cronos decides what is valid; the descriptor is more lenient, so it only fails on what Cronos already refused.
    private static string Describe(string expression)
    {
        try
        {
            return ExpressionDescriptor.GetDescription(expression, new Options
            {
                Locale = "en",
                Use24HourTimeFormat = true,
                ThrowExceptionOnParseError = true
            });
        }
        catch (FormatException)
        {
            throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Expression));
        }
    }

    private List<DateTimeOffset> NextRuns(CronExpression[] schedule, TimeZoneInfo zone, Args args)
    {
        var count = Math.Clamp(args.Count, 1, MaxCount);
        var runs = new List<DateTimeOffset>(count);
        var from = args.From ?? _time.GetUtcNow();

        // The earliest of the schedules' next runs; a moment both reach is listed once, since the next search starts after it.
        while (runs.Count < count && schedule.Select(it => it.GetNextOccurrence(from, zone)).Min() is { } next)
        {
            runs.Add(next);
            from = next;
        }

        return runs;
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Cron expression: five fields (minute, hour, day of month, month, day of week), six with seconds first, or a macro such as @daily")]
        string? Expression,
        [property: Description("Time zone the schedule runs in, as an IANA id such as Europe/London; UTC when omitted")]
        string? TimeZone = null,
        [property: Description("Moment after which run times are listed; the current time when omitted")]
        DateTimeOffset? From = null,
        [property: Description("How many run times to list; values below 1 are treated as 1, above 100 as 100")]
        int Count = 10
    );

    public sealed record Result(
        [property: Description("What the expression means, in English, with times on a 24-hour clock")]
        string Description,
        [property: Description("The expression as it was read: fields separated by single spaces, and a macro written out as the fields it stands for")]
        string Expression,
        [property: Description("Id of the time zone the run times are in")]
        string TimeZone,
        [property: Description("The next run times after From, earliest first, each with its UTC offset in the time zone; empty when the expression never matches")]
        IList<DateTimeOffset> NextRuns
    ) : ToolResult
    {
        public Result() : this(string.Empty, string.Empty, string.Empty, [])
        {
        }
    }
}
