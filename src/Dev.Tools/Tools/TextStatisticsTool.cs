using System.Globalization;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "text-statistics",
    Title = "Text Statistics",
    Description =
        """
        Counts the characters, words, lines and paragraphs in text, and measures its length in UTF-16
        code units and UTF-8 bytes.

        A character is what a reader sees as one: an emoji, or a letter with combining accents, counts
        once however many code points it takes. A word is a run of characters between whitespace that
        holds at least one letter or digit, so a lone dash is not a word, and text written without
        spaces - Chinese, Japanese, Thai - counts one word per run. Lines are counted the way an editor
        numbers them, so text ending in a line break has an empty last line; \r\n, \n, \r, form feed,
        next line and the Unicode line and paragraph separators all break a line. A paragraph is a block
        of lines that are not blank. Empty text has zero of everything.
        """,
    Usage =
        "Use this instead of counting by eye: characters, words and bytes are easy to miscount, and a string's length in characters, UTF-16 code units and UTF-8 bytes differ as soon as it holds an emoji or non-Latin text - which decides whether a value fits a column, a field or a message limit.",
    Aliases = ["wc"],
    Keywords = [Keyword.Text, Keyword.String],
    Categories = [Category.Text]
)]
public sealed class TextStatisticsTool : ToolBase<TextStatisticsTool.Args, TextStatisticsTool.Result>
{
    protected override Result Execute(Args args)
    {
        var text = args.Text;
        if (string.IsNullOrEmpty(text))
        {
            return new Result();
        }

        return new Result(
            Characters: new StringInfo(text).LengthInTextElements,
            NonWhitespaceCharacters: CountNonWhitespaceCharacters(text),
            Words: CountWords(text),
            Lines: CountLines(text),
            Paragraphs: CountParagraphs(text),
            Utf8Bytes: Encoding.UTF8.GetByteCount(text),
            Utf16Length: text.Length
        );
    }

    private static int CountNonWhitespaceCharacters(ReadOnlySpan<char> text)
    {
        var count = 0;
        while (!text.IsEmpty)
        {
            var length = StringInfo.GetNextTextElementLength(text);
            if (!text[..length].IsWhiteSpace())
            {
                count++;
            }

            text = text[length..];
        }

        return count;
    }

    private static int CountWords(string text)
    {
        var words = 0;
        var counted = false;
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
            {
                counted = false;
            }
            else if (!counted && Rune.IsLetterOrDigit(rune))
            {
                words++;
                counted = true;
            }
        }

        return words;
    }

    private static int CountLines(string text)
    {
        var lines = 0;
        foreach (var _ in text.AsSpan().EnumerateLines())
        {
            lines++;
        }

        return lines;
    }

    private static int CountParagraphs(string text)
    {
        var paragraphs = 0;
        var previousBlank = true;
        foreach (var line in text.AsSpan().EnumerateLines())
        {
            var blank = line.IsWhiteSpace();
            if (!blank && previousBlank)
            {
                paragraphs++;
            }

            previousBlank = blank;
        }

        return paragraphs;
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Text to measure; empty or omitted text counts as zero of everything")]
        string? Text
    );

    public sealed record Result(
        [property: Description("Characters as a reader sees them: an emoji or an accented letter counts as one")]
        int Characters,
        [property: DisplayName("Characters Excluding Whitespace"),
                   Description("Characters that are not whitespace, counted the same way as Characters")]
        int NonWhitespaceCharacters,
        [property: Description("Runs of characters between whitespace that hold at least one letter or digit")]
        int Words,
        [property: Description("Lines as an editor numbers them; text ending in a line break has an empty last line")]
        int Lines,
        [property: Description("Blocks of lines that are not blank, separated by one or more blank lines")]
        int Paragraphs,
        [property: DisplayName("UTF-8 Bytes"),
                   Description("Size of the text encoded as UTF-8, without a byte order mark")]
        int Utf8Bytes,
        [property: DisplayName("UTF-16 Length"),
                   Description("Length in UTF-16 code units, as .NET, Java and JavaScript report a string's length; an emoji counts as two or more")]
        int Utf16Length
    ) : ToolResult
    {
        public Result() : this(0, 0, 0, 0, 0, 0, 0)
        {
        }
    }
}
