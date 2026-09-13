using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class CronParserToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private CronParserTool _tool = null!;
    private CronParserTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _expression = string.Empty;

    // The browser's own time zone, so the run times read as the visitor's local time.
    private string _timeZone = TimeZoneInfo.Local.Id;
    private int _count = 10;

    [Inject] private WebContext Context { get; set; } = null!;

    private string? ExpressionError => ErrorFor(nameof(CronParserTool.Args.Expression));
    private string? TimeZoneError => ErrorFor(nameof(CronParserTool.Args.TimeZone));

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<CronParserToolPage>();
        _tool = Context.ToolsProvider.GetTool<CronParserTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<CronParserTool>();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_expression))
        {
            _result = null;
            return;
        }

        var args = new CronParserTool.Args(
            _expression,
            string.IsNullOrWhiteSpace(_timeZone) ? null : _timeZone,
            Count: _count);

        _result = await _tool.RunAsync(args, CancellationToken.None);
    }

    private async Task OnCountChangedAsync(int count)
    {
        _count = count;
        await OnValueChangedAsync();
    }

    private string? ErrorFor(string target)
        => _result?.Errors
            .Where(it => it.Target == target)
            .Select(it => Context.Localization.GetErrorCode(it.Code))
            .FirstOrDefault();
}
