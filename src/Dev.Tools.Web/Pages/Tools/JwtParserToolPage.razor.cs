using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class JwtParserToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private JwtParserTool _tool = null!;
    private JwtParserTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _token = string.Empty;

    [Inject] 
    private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<JwtParserToolPage>();
        _tool = Context.ToolsProvider.GetTool<JwtParserTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<JwtParserTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_token))
        {
            _result = null;
            return;
        }

        var args = new JwtParserTool.Args(_token);
        _result = await _tool.RunAsync(args, CancellationToken.None);
    }
}
