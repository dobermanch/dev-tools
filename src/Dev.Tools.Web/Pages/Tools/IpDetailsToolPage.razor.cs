using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class IpDetailsToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition;
    private IpDetailsTool _tool = null!;
    private readonly IpDetailsTool.Args _args = new();
    private IpDetailsTool.Result _result = new();

    [Inject] private WebContext Context { get; set; } = null!;
    
    protected override async Task OnInitializedAsync()
    {
        _tool = Context.ToolsProvider.GetTool<IpDetailsTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<IpDetailsTool>();
        
        _result = await _tool.RunAsync(_args, CancellationToken.None);

        await base.OnInitializedAsync();
    }

    private void NavigateToPreviousPage()
    {
        Context.Navigation.NavigateTo("/");
    }
}