using System.Text.RegularExpressions;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "regex-tester",
    Title = "Regex Tester",
    Description =
        """
        Tests a regular expression against text: lists every match with its position and capture
        groups, and replaces the matches when given a replacement.

        The pattern uses the .NET flavour, which differs from JavaScript and PCRE in places: named groups
        are (?<name>...), lookbehind may be any length, and a replacement refers to groups as $1 or
        ${name}. Matching ignores the culture, so IgnoreCase folds letters the same way on every machine.

        Finding one match may take at most 2 seconds, so a pattern that backtracks catastrophically is
        reported instead of running on. Positions are 0-based offsets in UTF-16 code units, with a
        1-based line and column. Every match is counted, and the first 1,000 are listed.
        """,
    Usage =
        "Run a pattern against real text here instead of predicting what it matches. It is the .NET flavour: a pattern meant for JavaScript, Python or grep may rely on syntax those engines do not share with .NET, such as (?<name>) groups, variable-length lookbehind and \\p{L} classes.",
    Aliases = ["regex"],
    Keywords = [Keyword.Regex, Keyword.Text, Keyword.String],
    Categories = [Category.Development, Category.Text]
)]
public sealed class RegexTesterTool(TimeSpan? matchTimeout = null)
    : ToolBase<RegexTesterTool.Args, RegexTesterTool.Result>
{
    private const int MaxListedMatches = 1_000;

    private readonly TimeSpan _matchTimeout = matchTimeout ?? TimeSpan.FromSeconds(2);

    protected override Result Execute(Args args, CancellationToken cancellationToken)
    {
        var errors = new List<ToolError>();
        if (string.IsNullOrEmpty(args.Text))
        {
            errors.Add(new ToolError(ErrorCode.TextEmpty, nameof(Args.Text)));
        }

        if (string.IsNullOrEmpty(args.Pattern))
        {
            errors.Add(new ToolError(ErrorCode.TextEmpty, nameof(Args.Pattern)));
        }

        if (errors.Count > 0)
        {
            throw new ToolException(errors);
        }

        Regex regex;
        try
        {
            regex = new Regex(args.Pattern, OptionsOf(args), _matchTimeout);
        }
        catch (ArgumentException)
        {
            throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Pattern));
        }

        var text = args.Text!;
        var lineStarts = LineStarts(text);
        var matches = new List<MatchInfo>();
        var values = new StringBuilder();
        var count = 0;

        try
        {
            for (var match = regex.Match(text); match.Success; match = match.NextMatch())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (count > 0)
                {
                    values.Append('\n');
                }

                values.Append(match.ValueSpan);
                if (count < MaxListedMatches)
                {
                    matches.Add(Describe(regex, match, lineStarts));
                }

                count++;
            }

            var output = args.Replacement is null ? values.ToString() : regex.Replace(text, args.Replacement);

            return new Result(output, count, matches);
        }
        catch (RegexMatchTimeoutException)
        {
            throw new ToolException(ErrorCode.MatchTimeout, nameof(Args.Pattern));
        }
    }

    private static RegexOptions OptionsOf(Args args)
    {
        var options = RegexOptions.CultureInvariant;
        if (args.IgnoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        if (args.Multiline)
        {
            options |= RegexOptions.Multiline;
        }

        if (args.Singleline)
        {
            options |= RegexOptions.Singleline;
        }

        if (args.IgnorePatternWhitespace)
        {
            options |= RegexOptions.IgnorePatternWhitespace;
        }

        return options;
    }

    // Where each line starts; \n ends a line, so \r\n does too.
    private static List<int> LineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (var index = text.IndexOf('\n'); index >= 0; index = text.IndexOf('\n', index + 1))
        {
            starts.Add(index + 1);
        }

        return starts;
    }

    private static MatchInfo Describe(Regex regex, Match match, List<int> lineStarts)
    {
        var line = lineStarts.BinarySearch(match.Index);
        if (line < 0)
        {
            // Not a line start itself: the line is the last one that starts before it.
            line = ~line - 1;
        }

        // Group 0 is the match itself.
        var groups = regex.GetGroupNumbers()
            .Where(number => number != 0)
            .Select(number => match.Groups[number] is { Success: true } group
                ? new GroupInfo(regex.GroupNameFromNumber(number), true, group.Index, group.Value)
                : new GroupInfo(regex.GroupNameFromNumber(number), false, null, null))
            .ToArray();

        return new MatchInfo(match.Index, line + 1, match.Index - lineStarts[line] + 1, match.Value, groups);
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Text to search")]
        string? Text,
        [property: Description("The .NET regular expression to match")]
        string Pattern,
        [property: Description("Text each match is replaced with, referring to groups as $1 or ${name}; without one nothing is replaced, and an empty one deletes the matches")]
        string? Replacement = null,
        [property: Description("Match letters regardless of case")]
        bool IgnoreCase = false,
        [property: Description("^ and $ match at the start and end of every line, not only of the whole text")]
        bool Multiline = false,
        [property: Description(". matches every character, line breaks included")]
        bool Singleline = false,
        [property: Description("Unescaped whitespace in the pattern is ignored and # starts a comment, for a pattern written over several lines")]
        bool IgnorePatternWhitespace = false
    );

    /// <summary>One match: where it starts, its text, and every capture group of the pattern.</summary>
    public sealed record MatchInfo(int Index, int Line, int Column, string Value, IList<GroupInfo> Groups);

    /// <summary>
    /// A capture group by its name, or its number when it has none. A group that took no part in the
    /// match is not successful and has no index or value.
    /// </summary>
    public sealed record GroupInfo(string Name, bool Success, int? Index, string? Value);

    public sealed record Result(
        [property: PipeOutput,
                   Description("With a Replacement, the text with every match replaced; otherwise the value of every match, one per line")]
        string Output,
        [property: Description("How many matches the pattern found in the whole text")]
        int MatchCount,
        [property: Description("The first 1,000 matches: each one's 0-based index, 1-based line and column, value, and capture groups")]
        IList<MatchInfo> Matches
    ) : ToolResult
    {
        public Result() : this(string.Empty, 0, [])
        {
        }
    }
}
