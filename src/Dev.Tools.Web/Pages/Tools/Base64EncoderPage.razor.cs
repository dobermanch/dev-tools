using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class Base64EncoderPage : ComponentBase
{

    private ToolDefinition _toolDefinition;
    private Base64EncoderTool _tool = null!;
    private readonly Base64EncoderTool.Args _args = new();
    private Base64EncoderTool.Result _result = new();

    [Inject] private WebContext Context { get; set; } = null!;

    protected override void OnInitialized()
    {
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