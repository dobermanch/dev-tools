using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class TextStatisticsToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private TextStatisticsTool _tool = null!;
    private TextStatisticsTool.Result _result = new();
    private IStringLocalizer _localizer = null!;

    private string _text = string.Empty;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<TextStatisticsToolPage>();
        _tool = Context.ToolsProvider.GetTool<TextStatisticsTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<TextStatisticsTool>();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        _result = await _tool.RunAsync(new TextStatisticsTool.Args(_text), CancellationToken.None);
    }
}
