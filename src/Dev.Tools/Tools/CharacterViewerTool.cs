using System.Globalization;
using System.Text.RegularExpressions;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "char-viewer",
    Aliases = [],
    Keywords = [Keyword.Text, Keyword.String],
    Categories = [Category.Text]
)]
public sealed class CharacterViewerTool : ToolBase<CharacterViewerTool.Args, CharacterViewerTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            return new Result(string.Empty, []);
        }

        HashSet<char> standardHexCodes = GetHexCodes(args);
        var result = new StringBuilder();
        var details = new Dictionary<char, CharInfo>();

        var unescaped = Regex.Unescape(args.Text);
        foreach (var ch in unescaped)
        {
            if (!details.TryGetValue(ch, out var info))
            {
                var hexCode = $"{(int)ch:X4}";
                var nonStandard = IsNonStandard(ch, args.ViewType is ViewType.RevealAll or ViewType.RemoveAll,
                    standardHexCodes);

                details[ch] = info = new CharInfo(
                    ch,
                    $"{hexCode}",
                    $"{(int)ch}",
                    CharUnicodeInfo.GetUnicodeCategory(ch).ToString(),
                    nonStandard ? $"\\u{hexCode}" : ch.ToString(),
                    nonStandard
                );
            }

            switch (args.ViewType)
            {
                case ViewType.RevealAll:
                    result.Append(info.Printable);
                    break;
                case ViewType.RevealNonStandard:
                    result.Append(info.NonStandard ? info.Printable : info.Char);
                    break;
                case ViewType.RemoveNonStandard:
                case ViewType.RemoveAll:
                    if (!info.NonStandard)
                    {
                        result.Append(info.Char);
                    }

                    break;
                default:
                    result.Append(info.Char);
                    break;
            }
        }

        return new Result(result.ToString(), args.IncludeCharInfo ? details.Values.ToArray() : []);
    }

    private static HashSet<char> GetHexCodes(Args args)
        => args
            .TreatAsStandardHexCodes
            ?.Select(it =>
            {
                var unescaped = Regex.Unescape(it);
                return unescaped.Length == 1
                    ? $"{(int)unescaped[0]:X4}"
                    : it.Replace("U+", "", StringComparison.InvariantCultureIgnoreCase)
                        .Replace("U", "", StringComparison.InvariantCultureIgnoreCase);
            })
            .Select(it => (char)int.Parse(it, NumberStyles.HexNumber))
            .ToHashSet() ?? [];

    private static bool IsNonStandard(char ch, bool showAll, HashSet<char> standardHexCodes)
    {
        // Ignore common ASCII control characters
        if (!showAll && (ch is '\n' or '\r' or '\t' or '\b' or '\f' || standardHexCodes.Contains(ch)))
        {
            return false;
        }

        // Ignore common letters
        var category = CharUnicodeInfo.GetUnicodeCategory(ch);
        if (category is
            UnicodeCategory.UppercaseLetter or
            UnicodeCategory.LowercaseLetter or
            UnicodeCategory.TitlecaseLetter or
            UnicodeCategory.ModifierLetter or
            UnicodeCategory.LetterNumber or
            UnicodeCategory.OtherLetter)
        {
            return false;
        }

        return ch < 0x20 || ch > 0x7E || CharUnicodeInfo.GetUnicodeCategory(ch) is
            UnicodeCategory.Control or
            UnicodeCategory.Format or
            UnicodeCategory.Surrogate or
            UnicodeCategory.PrivateUse;
    }

    public enum ViewType
    {
        None = 0,
        RevealAll,
        RevealNonStandard,
        RemoveAll,
        RemoveNonStandard
    }

    public readonly record struct Args(
        [property: PipeInput] string Text,
        ViewType ViewType = ViewType.RevealNonStandard,
        bool IncludeCharInfo = false,
        IList<string>? TreatAsStandardHexCodes = null
    );

    public readonly record struct CharInfo(
        char Char,
        string HexCode,
        string AsciiCode,
        string UnicodeCategory,
        string Printable,
        bool NonStandard
    )
    {
        public readonly string UnicodeEscapedString = $"\\u{HexCode}";
        public readonly string UnicodeDisplayString = $"U+{HexCode}";
        public readonly string HtmlString = $"&#{AsciiCode};";
        public readonly string UrlString = Uri.EscapeDataString(Char.ToString());
    }

    public sealed record Result(
        [property: PipeOutput] string Text,
        IList<CharInfo> CharInfos
    ) : ToolResult
    {
        public Result() : this("", [])
        {
        }
    }
}