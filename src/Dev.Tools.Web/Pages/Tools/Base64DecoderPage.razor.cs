using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class Base64DecoderPage : ComponentBase
{
    private ToolDefinition _toolDefinition;
    private Base64DecoderTool _tool = null!;
    private readonly Base64DecoderTool.Args _args = new();
    private Base64DecoderTool.Result _result = new();

    [Inject] private WebContext Context { get; set; } = null!;

    protected override void OnInitialized()
    {
        _tool = Context.ToolsProvider.GetTool<Base64DecoderTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<Base64DecoderTool>();

        base.OnInitialized();
    }

    private async Task OnDecodeStringValueChangedAsync(string value)
    {
        _args.Text = value;
        _result = await _tool.RunAsync(_args, CancellationToken.None);
    }
}