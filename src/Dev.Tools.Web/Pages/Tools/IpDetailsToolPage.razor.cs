using Dev.Tools.Providers;
using Dev.Tools.Tools;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class IpDetailsToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition;
    private IpDetailsTool _tool = null!;
    private readonly IpDetailsTool.Args _args = new();
    private IpDetailsTool.Result _result = new();

    [Inject] private IToolsProvider Provider { get; set; } = null!;
    
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;
    
    protected override async Task OnInitializedAsync()
    {
        _tool = Provider.GetTool<IpDetailsTool>();
        _toolDefinition = Provider.GetToolDefinition<IpDetailsTool>();
        
        _result = await _tool.RunAsync(_args, CancellationToken.None);

        await base.OnInitializedAsync();
    }

    private void NavigateToPreviousPage()
    {
        Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
        Navigation.NavigateTo("javascript:history.back()");
    }
}