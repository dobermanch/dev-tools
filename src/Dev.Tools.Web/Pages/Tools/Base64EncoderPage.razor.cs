using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class Base64EncoderPage : ComponentBase
{

    private ToolDefinition _toolDefinition = null!;
    private Base64EncoderTool _tool = null!;
    private readonly Base64EncoderTool.Args _args = new();
    private Base64EncoderTool.Result _result = new();
    private IStringLocalizer _localizer = null!;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.PageLocalizer<Base64EncoderPage>();
        _tool = Context.ToolsProvider.GetTool<Base64EncoderTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<Base64EncoderTool>();

        base.OnInitialized();
    }

    private async Task OnStringToEncodeValueChangedAsync(string value)
    {
        _args.Text = value;
        _result = await _tool.RunAsync(_args, CancellationToken.None);
    }
}