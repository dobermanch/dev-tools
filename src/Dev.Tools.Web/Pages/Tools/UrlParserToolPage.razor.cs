using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class UrlParserToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private UrlParserTool _tool = null!;
    private UrlParserTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _url = string.Empty;

    [Inject] private WebContext Context { get; set; } = null!;

    private string? FirstErrorText => _result?.Errors
        .Select(it => Context.Localization.GetErrorCode(it.Code))
        .FirstOrDefault();

    // What copy and download act on: every part at once, labelled.
    private string AllParts => _result is null or { HasErrors: true }
        ? string.Empty
        : string.Join(Environment.NewLine, new[]
        {
            $"{_localizer["TextField.Schema.Label"]}: {_result.Schema}",
            $"{_localizer["TextField.Hostname.Label"]}: {_result.Hostname}",
            _result.Port is null ? null : $"{_localizer["TextField.Port.Label"]}: {_result.Port}",
            $"{_localizer["TextField.Path.Label"]}: {_result.Path}",
            string.IsNullOrEmpty(_result.Username) ? null : $"{_localizer["TextField.Username.Label"]}: {_result.Username}",
            string.IsNullOrEmpty(_result.Query) ? null : $"{_localizer["TextField.Query.Label"]}: {_result.Query}"
        }.Where(line => line is not null));

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<UrlParserToolPage>();
        _tool = Context.ToolsProvider.GetTool<UrlParserTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<UrlParserTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_url))
        {
            _result = null;
            return;
        }

        var args = new UrlParserTool.Args(_url);
        _result = await _tool.RunAsync(args, CancellationToken.None);
    }
}
