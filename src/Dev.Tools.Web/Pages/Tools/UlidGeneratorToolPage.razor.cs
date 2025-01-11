using Dev.Tools.Providers;
using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class UlidGeneratorToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition;
    private UlidGeneratorTool _tool = null!;
    private readonly UlidGeneratorTool.Args _args = new();
    private UlidGeneratorTool.Result? _result;

    [Inject] private IToolsProvider Provider { get; set; } = null!;
    
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;
    
    [Inject]
    private IJsServices JsServices { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _tool = Provider.GetTool<UlidGeneratorTool>();
        _toolDefinition = Provider.GetToolDefinition<UlidGeneratorTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        _result = await _tool.RunAsync(_args, CancellationToken.None);
    }

    private string GetUlids()
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
    
    private Task OnTypeValueChangedAsync(UlidGeneratorTool.UlidType type)
    {
        _args.Type = type;
        return OnValueChangedAsync();
    }
}