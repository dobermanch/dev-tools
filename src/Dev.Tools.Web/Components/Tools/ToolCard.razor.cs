using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Tools;

public partial class ToolCard : ComponentBase
{
    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter] 
    public ToolDefinition Tool { get; set; } = null!;
    
    private void NavigateToToolPage()
    {
        Context.Navigation.NavigateTo($"/tools/{Tool.Name}");
    }
}