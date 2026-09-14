using System.Globalization;
using DiffPlex;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "text-diff",
    Title = "Text Diff",
    Description =
        """
        Compares two texts line by line and shows the differences as a unified diff, the format diff -u and
        git diff write.

        The modified text is compared against Original: a line only in the original is removed (-), a line
        only in the modified text is added (+), and a changed line is a removal followed by an addition.
        Each group of changes is shown with ContextLines unchanged lines around it, and groups closer than
        that share one hunk. Line endings are not compared, so text with Windows line breaks matches the same
        text with Unix ones; a missing line break at the very end is a change, marked
        "\ No newline at end of file".

        IgnoreWhitespace compares lines with every space and tab removed, and IgnoreCase compares them
        regardless of letter case. Lines are still shown as written, an unchanged line as it is in the
        original. Identical texts give an empty diff.
        """,
    Usage =
        "Use this to find exactly what changed between two versions of a text instead of comparing them by eye, or to produce a patch-style summary of a change. It compares whole lines - a one-character edit shows as the line removed and added - and it does not merge or apply patches.",
    Aliases = ["diff"],
    Keywords = [Keyword.Diff, Keyword.Text, Keyword.String],
    Categories = [Category.Development, Category.Text]
)]
public sealed class TextDiffTool : ToolBase<TextDiffTool.Args, TextDiffTool.Result>
{
    private const string NoNewlineMarker = "\\ No newline at end of file";

    protected override Result Execute(Args args)
    {
        var original = Document.Read(args.Original);
        var modified = Document.Read(args.Modified);
        var lines = Compare(original, modified, args);

        return new Result(
            Diff: Render(lines, original, modified, Math.Max(args.ContextLines, 0)),
            HasChanges: lines.Any(it => it.Kind != DiffLineKind.Unchanged),
            AddedLines: lines.Count(it => it.Kind == DiffLineKind.Added),
            RemovedLines: lines.Count(it => it.Kind == DiffLineKind.Removed),
            Lines: lines);
    }

    // DiffPlex finds the differences between the pieces; the pieces are the lines as compared, so the ignore
    // options and the final line break are decided here, and block positions still index the lines as written.
    private static List<DiffLine> Compare(Document original, Document modified, Args args)
    {
        var diff = Differ.Instance.CreateCustomDiffs(
            ComparedText(original, args), ComparedText(modified, args), false, false, Pieces);

        var lines = new List<DiffLine>();
        var o = 0;
        var m = 0;
        foreach (var block in diff.DiffBlocks)
        {
            for (; o < block.DeleteStartA; o++, m++)
            {
                lines.Add(new DiffLine(DiffLineKind.Unchanged, o + 1, m + 1, original.Lines[o]));
            }

            for (var i = 0; i < block.DeleteCountA; i++, o++)
            {
                lines.Add(new DiffLine(DiffLineKind.Removed, o + 1, null, original.Lines[o]));
            }

            for (var i = 0; i < block.InsertCountB; i++, m++)
            {
                lines.Add(new DiffLine(DiffLineKind.Added, null, m + 1, modified.Lines[m]));
            }
        }

        for (; o < original.Lines.Length; o++, m++)
        {
            lines.Add(new DiffLine(DiffLineKind.Unchanged, o + 1, m + 1, original.Lines[o]));
        }

        return lines;
    }

    // One piece per line, each behind a '|' so an empty line is still a piece, joined by '\n', which no line
    // contains. The last line gains a mark when no line break follows it, so that difference is a change.
    private static string ComparedText(Document document, Args args)
        => string.Join('\n', document.Lines.Select((line, index) =>
        {
            var compared = args.IgnoreWhitespace ? string.Concat(line.Where(it => !char.IsWhiteSpace(it))) : line;
            compared = args.IgnoreCase ? compared.ToUpperInvariant() : compared;
            var unterminated = index == document.Lines.Length - 1 && !document.EndsWithNewline;

            return "|" + compared + (unterminated ? "\u0001" : string.Empty);
        }));

    private static string[] Pieces(string text) => text.Length == 0 ? [] : text.Split('\n');

    // Hunks as GNU diff builds them: each change with `context` lines either side, merged where they touch.
    private static string Render(List<DiffLine> lines, Document original, Document modified, int context)
    {
        var changes = Enumerable.Range(0, lines.Count).Where(i => lines[i].Kind != DiffLineKind.Unchanged).ToList();
        if (changes.Count == 0)
        {
            return string.Empty;
        }

        var text = new StringBuilder("--- original\n+++ modified\n");
        var next = 0;
        while (next < changes.Count)
        {
            var start = Math.Max(0, changes[next] - context);
            var end = Math.Min(lines.Count - 1, changes[next] + context);
            while (next + 1 < changes.Count && changes[next + 1] - context <= end + 1)
            {
                next++;
                end = Math.Min(lines.Count - 1, changes[next] + context);
            }

            next++;
            AppendHunk(text, lines, start, end, original, modified);
        }

        // Without the final line break, as every tool's text output: the CLI adds one when it prints.
        return text.ToString(0, text.Length - 1);
    }

    private static void AppendHunk(StringBuilder text, List<DiffLine> lines, int start, int end, Document original, Document modified)
    {
        var hunk = lines.GetRange(start, end - start + 1);
        var originalBefore = lines.Take(start).Count(it => it.OriginalLine is not null);
        var modifiedBefore = lines.Take(start).Count(it => it.ModifiedLine is not null);
        var originalCount = hunk.Count(it => it.OriginalLine is not null);
        var modifiedCount = hunk.Count(it => it.ModifiedLine is not null);

        text.Append("@@ -").Append(Range(originalBefore, originalCount))
            .Append(" +").Append(Range(modifiedBefore, modifiedCount))
            .Append(" @@\n");

        foreach (var line in hunk)
        {
            text.Append(line.Kind switch
                {
                    DiffLineKind.Removed => '-',
                    DiffLineKind.Added => '+',
                    _ => ' '
                })
                .Append(line.Text)
                .Append('\n');

            // An unchanged last line lacks its line break in both texts, or it would not have matched.
            var unterminated = line.Kind == DiffLineKind.Added
                ? line.ModifiedLine == modified.Lines.Length && !modified.EndsWithNewline
                : line.OriginalLine == original.Lines.Length && !original.EndsWithNewline;
            if (unterminated)
            {
                text.Append(NoNewlineMarker).Append('\n');
            }
        }
    }

    // "start,count", the start being the line before the hunk when it holds no lines of that text, and
    // the count left out when it is 1.
    private static string Range(int linesBefore, int count)
        => count switch
        {
            0 => string.Create(CultureInfo.InvariantCulture, $"{linesBefore},0"),
            1 => (linesBefore + 1).ToString(CultureInfo.InvariantCulture),
            _ => string.Create(CultureInfo.InvariantCulture, $"{linesBefore + 1},{count}")
        };

    private sealed record Document(string[] Lines, bool EndsWithNewline)
    {
        // \r\n, \n and \r all end a line, so line endings are not part of what is compared.
        public static Document Read(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new Document([], true);
            }

            var lines = new List<string>();
            var start = 0;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] is not ('\r' or '\n'))
                {
                    continue;
                }

                lines.Add(text[start..i]);
                if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                start = i + 1;
            }

            var endsWithNewline = start == text.Length;
            if (!endsWithNewline)
            {
                lines.Add(text[start..]);
            }

            return new Document(lines.ToArray(), endsWithNewline);
        }
    }

    public enum DiffLineKind
    {
        [Description("In both texts")]
        Unchanged,

        [Description("Only in the original text")]
        Removed,

        [Description("Only in the modified text")]
        Added
    }

    /// <summary>
    /// One line of the comparison, numbered from 1 in each text it is in. An unchanged line carries its text
    /// as it is in the original.
    /// </summary>
    public sealed record DiffLine(DiffLineKind Kind, int? OriginalLine, int? ModifiedLine, string Text);

    public sealed record Args(
        [property: PipeInput,
                   Description("The changed text, compared against Original")]
        string? Modified,
        [property: Description("The text as it was before the change")]
        string? Original = null,
        [property: Description("Compare lines with every space and tab removed")]
        bool IgnoreWhitespace = false,
        [property: Description("Compare lines regardless of letter case")]
        bool IgnoreCase = false,
        [property: Description("Unchanged lines shown around each group of changes; values below 0 are treated as 0")]
        int ContextLines = 3
    );

    public sealed record Result(
        [property: PipeOutput,
                   Description("The differences in unified diff format, as diff -u and git diff write them but without the final line break; empty when the texts match")]
        string Diff,
        [property: Description("Whether the texts differ, beyond what the ignore options leave out")]
        bool HasChanges,
        [property: Description("How many lines are only in the modified text")]
        int AddedLines,
        [property: Description("How many lines are only in the original text")]
        int RemovedLines,
        [property: Description("Every line of both texts in order, each unchanged, removed or added, with its line number in each text it is in")]
        IList<DiffLine> Lines
    ) : ToolResult
    {
        public Result() : this(string.Empty, false, 0, 0, [])
        {
        }
    }
}
