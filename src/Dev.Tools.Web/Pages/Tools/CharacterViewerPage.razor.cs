using System.Globalization;
using System.Text.RegularExpressions;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class CharacterViewerPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private CharacterViewerTool _tool = null!;
    private readonly Args _args = new();
    private CharacterViewerTool.Result _result = new();
    private Dictionary<char, CharacterViewerTool.CharInfo> _charMap = new();
    private bool _showSymbols;
    private IStringLocalizer _localizer = null!;

    private bool HideSymbolsOptions =>
        _args.ViewType is CharacterViewerTool.ViewType.RevealNonStandard 
            or CharacterViewerTool.ViewType.RemoveNonStandard;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.PageLocalizer<CharacterViewerPage>();
        _tool = Context.ToolsProvider.GetTool<CharacterViewerTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<CharacterViewerTool>();

        base.OnInitialized();
    }

    private async Task OnTextValueChangedAsync(string text)
    {
        _args.Text = text;
        await RunCommandAsync();
    }

    private async Task OnViewTypeChangedAsync(CharacterViewerTool.ViewType arg)
    {
        _args.ViewType = arg;
        await RunCommandAsync();
    }

    private async Task RunCommandAsync()
    {
        _result = await _tool.RunAsync(new CharacterViewerTool.Args(
                _args.Text,
                _args.ViewType,
                _args.IncludeCharInfo,
                _args.TreatAsStandardHexCodes),
            CancellationToken.None);
        _charMap = _result.CharInfos.ToDictionary(it => it.Char, it => it);
        StateHasChanged();
    }

    private MarkupString FormatText(string input)
    {
        var builder = new System.Text.StringBuilder();

        var unescaped = Regex.Unescape(input);
        foreach (char ch in unescaped)
        {
            var style = GetStyle(ch);
            var print = style is not null 
                ? $"<span style=\"{style}\" title=\"{DescribeChar(ch)}\">{CharToDisplay(ch)}</span>" 
                : CharToDisplay(ch);
            
            builder.Append(print);
        }

        return (MarkupString)builder.ToString();
    }

    private string CharToDisplay(char c)
    {
        if (!_showSymbols && !IsNonStandard(c))
        {
            return _charMap.TryGetValue(c, out var info) ? info.NonStandard ? "" : info.Printable : c.ToString();
        }

        return c switch
        {
            '\n' => "\u21b5\n",  // ↵ + newline
            '\r' => "\u240d",    // ␍ Carriage Return
            '\t' => "\u2192\t",  // → + tab
            '\b' => "\u2408",    // ␈ Backspace
            '\f' => "\u240c",    // ␌ Form Feed
            '\0' => "\u2400",    // ␀ Null
            ' '  => "\u00b7",    // · Middle dot for space
            '\u00A0' => "\u237d", // ⍽ Non-breaking space
            '\u200B' => "\u200b\u20da", // Zero-width space (show marker)
            '\u200C' => "\u200c\u20e3", // Zero-width non-joiner
            '\u200D' => "\u200d\u20e3", // Zero-width joiner
            '\u2060' => "\u2060\u20e3", // Word joiner
            '\uFEFF' => "[BOM]",        // Byte order mark
            '\u202F' => "\u237d",       // Narrow no-break space
            _ => IsNonStandard(c)
                ? $"\\u{(int)c:X4}"
                : c.ToString()
        };
    }

    private bool IsNonStandard(char c)
    {
        if (_charMap.TryGetValue(c, out var info))
            return info.NonStandard;

        return char.IsControl(c) || char.GetUnicodeCategory(c) is
            UnicodeCategory.Format or
            UnicodeCategory.Surrogate or
            UnicodeCategory.PrivateUse;
    }

    private string DescribeChar(char c) => ((int)c) switch
    {
        0x0000 => "Null (NUL)",
        0x0008 => "Backspace (BS)",
        0x0009 => "Tab (HT)",
        0x000A => "Line Feed (LF)",
        0x000B => "Vertical Tab (VT)",
        0x000C => "Form Feed (FF)",
        0x000D => "Carriage Return (CR)",
        0x001B => "Escape (ESC)",
        0x0020 => "Space (SP)",
        0x00A0 => "Non-breaking space (NBSP)",
        0x200B => "Zero-width space (ZWSP)",
        0x200C => "Zero-width non-joiner (ZWNJ)",
        0x200D => "Zero-width joiner (ZWJ)",
        0x2028 => "Line separator",
        0x2029 => "Paragraph separator",
        0x202F => "Narrow no-break space (NNBSP)",
        0x2060 => "Word joiner (WJ)",
        0xFEFF => "Byte order mark (BOM)",
        _ when char.IsControl(c) => $"Control character (U+{(int)c:X4})",
        _ when IsNonStandard(c) => $"Non-standard (U+{(int)c:X4})",
        _ => $"U+{(int)c:X4}"
    };

    private string? GetStyle(char c)
    {
        if (!_showSymbols && !IsNonStandard(c))
        {
            return null;
        }

        if (char.IsControl(c) || IsNonStandard(c))
        {
            return "background-color: var(--mud-palette-warning-lighten); color: var(--mud-palette-error-darken); padding: 0 2px; border-radius: 2px; font-weight: bold;";
        }

        return c == ' ' ? "color: var(--mud-palette-text-disabled);" : null;
    }

    public sealed record Args
    {
        public string Text { get; set; } = "";
        public CharacterViewerTool.ViewType ViewType { get; set; } = CharacterViewerTool.ViewType.RevealNonStandard;
        public bool IncludeCharInfo { get; set; } = true;
        public IList<string> TreatAsStandardHexCodes { get; set; } = [];
    }
}
