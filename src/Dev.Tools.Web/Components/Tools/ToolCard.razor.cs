using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Tools;

public partial class ToolCard : ComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;
    
    [Parameter]
    public ToolDefinition Tool { get; set; }

    private void NavigateToToolPage()
    {
        Navigation.NavigateTo($"/tools/{Tool.Name}");
    }
}