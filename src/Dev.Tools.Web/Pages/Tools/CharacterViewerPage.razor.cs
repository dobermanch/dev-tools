using System.Text.RegularExpressions;
using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class CharacterViewerPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private CharacterViewerTool _tool = null!;
    private readonly Args _args = new();
    private CharacterViewerTool.Result _result = new();
    private Dictionary<char, CharacterViewerTool.CharInfo> _charMap = new();
    private bool _hideSymbols = false;

    private bool HideSymbolsOptions =>
        _args.ViewType is not CharacterViewerTool.ViewType.RemoveAll
            and CharacterViewerTool.ViewType.RemoveNonStandard;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override void OnInitialized()
    {
        _tool = Context.ToolsProvider.GetTool<CharacterViewerTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<CharacterViewerTool>();

        base.OnInitialized();
    }

    private async Task OnCopyToClipboardAsync(string? textToCopy)
    {
        if (!string.IsNullOrEmpty(textToCopy))
        {
            await Context.JsService.CopyToClipboardAsync(textToCopy);
        }
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
    
    private string FormatText(string input)
    {
        var builder = new System.Text.StringBuilder();

        var unescaped = Regex.Unescape(input);
        foreach (char ch in unescaped)
        {
            builder.Append(CharToDisplay(ch));
        }

        return builder.ToString();
    }

    private string CharToDisplay(char c)
    {
        if (_hideSymbols)
        {
            return _charMap.TryGetValue(c, out var value1) ? value1.NonStandard ? "" : value1.Printable : c.ToString();
        }

        if (_args.ViewType != CharacterViewerTool.ViewType.RevealAll)
        {
            var result = c switch
            {
                '\n' => "↵\n", // Line Feed (LF)
                '\r' => "␍", // Carriage Return (CR)
                '\t' => "→", // Horizontal Tab
                '\b' => "⇤", // Backspace
                '\f' => "␌", // Form Feed
                ' ' => "·", // Space
                '\u00A0' => "⍽", // Non-breaking space
                '\u2000' => " ", // En quad
                '\u2001' => " ", // Em quad
                '\u2002' => " ", // En space
                '\u2003' => " ", // Em space
                '\u2004' => " ", // Three-per-em space
                '\u2005' => " ", // Four-per-em space
                '\u2006' => " ", // Six-per-em space
                '\u2007' => " ", // Figure space
                '\u2008' => " ", // Punctuation space
                '\u2009' => " ", // Thin space
                '\u200A' => " ", // Hair space
                '\u200B' => "⁚", // Zero-width space
                '\u202F' => " ", // Narrow no-break space
                '\u2060' => "⏸", // Word joiner
                '\uFEFF' => "﻿", // Zero-width no-break space (BOM)
                '\\' => "⧵", // Backslash
                _ => _charMap.TryGetValue(c, out var value1) ? value1.Printable : c.ToString()
            };
        }

        return _charMap.TryGetValue(c, out var value) ? value.Printable : c.ToString();
    }

    private string DescribeChar(char c) => ((int)c) switch
    {
        0x000A => "Line Feed (LF)",
        0x000D => "Carriage Return (CR)",
        0x0009 => "Tab",
        0x0008 => "Backspace",
        0x000C => "Form Feed",
        0x00A0 => "Non-breaking space",
        0x200B => "Zero-width space",
        _ => char.IsControl(c) ? "Control character" : "Visible character"
    };

    private string GetStyle(char c)
    {
        if (char.IsControl(c) || c == '\u200B' || c == '\u00A0')
            return "background-color: #ffeb3b; color: #d32f2f; padding: 0 2px; border-radius: 2px;";
        if (c == ' ')
            return "color: #9e9e9e;";
        return "";
    }


    public sealed record Args
    {
        public string Text { get; set; } = "";
        public CharacterViewerTool.ViewType ViewType { get; set; } = CharacterViewerTool.ViewType.RevealNonStandard;
        public bool IncludeCharInfo { get; set; } = true;
        public IList<string> TreatAsStandardHexCodes { get; set; } = [];
    }
}