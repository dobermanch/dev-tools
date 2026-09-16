using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class TextDiffToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private TextDiffTool _tool = null!;
    private TextDiffTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _original = string.Empty;
    private string _modified = string.Empty;
    private bool _ignoreWhitespace;
    private bool _ignoreCase;
    private int _contextLines = 3;

    [Inject] private WebContext Context { get; set; } = null!;

    private string? FirstErrorText => _result?.Errors
        .Select(it => Context.Localization.GetErrorCode(it.Code))
        .FirstOrDefault();

    /// <summary>The error, the tally of what changed, or that nothing did.</summary>
    private string? DiffStatus => _result switch
    {
        null => null,
        { HasErrors: true } => FirstErrorText,
        { HasChanges: true } => _localizer["Text.Summary", _result.AddedLines, _result.RemovedLines],
        _ => _localizer["Text.NoChanges"]
    };

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<TextDiffToolPage>();
        _tool = Context.ToolsProvider.GetTool<TextDiffTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<TextDiffTool>();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrEmpty(_original) && string.IsNullOrEmpty(_modified))
        {
            _result = null;
            return;
        }

        _result = await _tool.RunAsync(
            new TextDiffTool.Args(_modified, _original, _ignoreWhitespace, _ignoreCase, _contextLines),
            CancellationToken.None);
    }

    private async Task SetOptionAsync(Action set)
    {
        set();
        await OnValueChangedAsync();
    }

    // Translucent, so they read on the light and the dark theme alike.
    private static string LineStyle(TextDiffTool.DiffLineKind kind) => kind switch
    {
        TextDiffTool.DiffLineKind.Added => "background-color: rgba(46, 160, 67, 0.18);",
        TextDiffTool.DiffLineKind.Removed => "background-color: rgba(248, 81, 73, 0.18);",
        _ => string.Empty
    };

    private static string Marker(TextDiffTool.DiffLineKind kind) => kind switch
    {
        TextDiffTool.DiffLineKind.Added => "+",
        TextDiffTool.DiffLineKind.Removed => "-",
        _ => string.Empty
    };
}
