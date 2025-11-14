using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class IpDetailsToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private IpDetailsTool _tool = null!;
    private readonly IpDetailsTool.Args _args = new();
    private IpDetailsTool.Result _result = new();
    private IStringLocalizer _localizer = null!;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<IpDetailsToolPage>();
        _tool = Context.ToolsProvider.GetTool<IpDetailsTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<IpDetailsTool>();
        
        _result = await _tool.RunAsync(_args, CancellationToken.None);

        await base.OnInitializedAsync();
    }
}