using Dev.Tools.Providers;
using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class UuidGeneratorToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition;
    private UuidGeneratorTool _tool = null!;
    private readonly UuidGeneratorTool.Args _args = new();
    private UuidGeneratorTool.Result? _result;

    [Inject] private IToolsProvider Provider { get; set; } = null!;
    
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;
    
    [Inject]
    private IJsServices JsServices { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _tool = Provider.GetTool<UuidGeneratorTool>();
        _toolDefinition = Provider.GetToolDefinition<UuidGeneratorTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        _result = await _tool.RunAsync(_args, CancellationToken.None);
    }

    private string GetUuids()
    {
        return _result != null ? string.Join(Environment.NewLine, _result.Data) : string.Empty;
    }

    private void NavigateToPreviousPage()
    {
        Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
        Navigation.NavigateTo("javascript:history.back()");
    }

    private async Task OnCopyToClipboardAsync(string? textToCopy)
    {
        if (!string.IsNullOrEmpty(textToCopy))
        {
            await JsServices.CopyToClipboardAsync(textToCopy);
        }
    }

    private Task OnCountValueChangedAsync(int count)
    {
        _args.Count = count;
        return OnValueChangedAsync();
    }
    
    private Task OnTypeValueChangedAsync(UuidGeneratorTool.UuidType type)
    {
        _args.Type = type;
        return OnValueChangedAsync();
    }
}