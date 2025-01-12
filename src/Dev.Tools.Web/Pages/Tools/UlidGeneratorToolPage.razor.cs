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

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _tool = Context.ToolsProvider.GetTool<UlidGeneratorTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<UlidGeneratorTool>();
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
        Context.Navigation.NavigateTo("/");
    }

    private async Task OnCopyToClipboardAsync(string? textToCopy)
    {
        if (!string.IsNullOrEmpty(textToCopy))
        {
            await Context.JsService.CopyToClipboardAsync(textToCopy);
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