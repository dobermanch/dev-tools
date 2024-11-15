using Dev.Tools.Providers;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Tools;

public partial class ToolCard : ComponentBase
{
    [Parameter]
    public ToolDefinition Tool { get; set; } = default!;
}