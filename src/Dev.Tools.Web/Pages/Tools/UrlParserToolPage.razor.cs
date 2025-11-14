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
