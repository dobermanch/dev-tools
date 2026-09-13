using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class RegexTesterToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private RegexTesterTool _tool = null!;
    private RegexTesterTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _pattern = string.Empty;
    private string _replacement = string.Empty;
    private string _text = string.Empty;
    private bool _ignoreCase;
    private bool _multiline;
    private bool _singleline;
    private bool _ignorePatternWhitespace;

    [Inject] private WebContext Context { get; set; } = null!;

    // A pattern that does not parse or takes too long is the pattern field's error.
    private string? PatternError => ErrorFor(nameof(RegexTesterTool.Args.Pattern));
    private string? TextError => ErrorFor(nameof(RegexTesterTool.Args.Text));

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<RegexTesterToolPage>();
        _tool = Context.ToolsProvider.GetTool<RegexTesterTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<RegexTesterTool>();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrEmpty(_pattern) || string.IsNullOrEmpty(_text))
        {
            _result = null;
            return;
        }

        // An empty field is no replacement: the page lists matches rather than deleting them.
        var args = new RegexTesterTool.Args(
            _text,
            _pattern,
            string.IsNullOrEmpty(_replacement) ? null : _replacement,
            _ignoreCase,
            _multiline,
            _singleline,
            _ignorePatternWhitespace);

        _result = await _tool.RunAsync(args, CancellationToken.None);
    }

    private async Task SetOptionAsync(Action set)
    {
        set();
        await OnValueChangedAsync();
    }

    private static string DescribeGroups(RegexTesterTool.MatchInfo match)
        => string.Join(", ", match.Groups.Select(group => $"{group.Name} = {(group.Success ? group.Value : "—")}"));

    private string? ErrorFor(string target)
        => _result?.Errors
            .Where(it => it.Target == target)
            .Select(it => Context.Localization.GetErrorCode(it.Code))
            .FirstOrDefault();
}
