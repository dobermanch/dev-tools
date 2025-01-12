using Dev.Tools.Providers;
using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class Base64ConverterPage : ComponentBase
{
    private ToolDefinition _decoderToolDefinition;
    private Base64DecoderTool _decoderTool = null!;
    private readonly Base64DecoderTool.Args _decoderArgs = new();
    private Base64DecoderTool.Result _decoderResult = new();

    private ToolDefinition _encoderToolDefinition;
    private Base64EncoderTool _encoderTool = null!;
    private readonly Base64EncoderTool.Args _encoderArgs = new();
    private Base64EncoderTool.Result _encoderResult = new();

    [Inject] private IToolsProvider Provider { get; set; } = null!;
    
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;
    
    [Inject]
    private IJsServices JsServices { get; set; } = null!;

    protected override void OnInitialized()
    {
        _decoderTool = Provider.GetTool<Base64DecoderTool>();
        _decoderToolDefinition = Provider.GetToolDefinition<Base64DecoderTool>();

        _encoderTool = Provider.GetTool<Base64EncoderTool>();
        _encoderToolDefinition = Provider.GetToolDefinition<Base64EncoderTool>();

        base.OnInitialized();
    }

    private async Task OnStringToEncodeValueChangedAsync(string value)
    {
        _encoderArgs.Text = value;
        _encoderResult = await _encoderTool.RunAsync(_encoderArgs, CancellationToken.None);
    }

    private async Task OnDecodeStringValueChangedAsync(string value)
    {
        _decoderArgs.Text = value;
        _decoderResult = await _decoderTool.RunAsync(_decoderArgs, CancellationToken.None);
    }

    private void NavigateToPreviousPage()
    {
        Navigation.NavigateTo("/");
    }

    private async Task OnCopyToClipboardAsync(string? textToCopy)
    {
        if (!string.IsNullOrEmpty(textToCopy))
        {
            await JsServices.CopyToClipboardAsync(textToCopy);
        }
    }
}