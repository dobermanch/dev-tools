using Dev.Tools.Providers;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Tools;

public partial class ToolsList : ComponentBase
{
    [Inject] private IToolsProvider ToolsProvider { get; set; } = default!;

    private GroupedList<string, ToolDefinition> Tools { get; set; } = GroupedList<string, ToolDefinition>.Empty;

    protected override void OnInitialized()
    {
        var tools = ToolsProvider.GetToolDefinitions();
            
        Tools = new GroupedList<string, ToolDefinition>(it => it.Name, tools);
        
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        
        base.OnParametersSet();
    }
}
