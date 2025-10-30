using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class UuidGeneratorToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private UuidGeneratorTool _tool = null!;
    private readonly UuidGeneratorTool.Args _args = new();
    private UuidGeneratorTool.Result? _result;

    [Inject] private WebContext Context { get; set; } = null!;
    
    protected override async Task OnInitializedAsync()
    {
        _tool = Context.ToolsProvider.GetTool<UuidGeneratorTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<UuidGeneratorTool>();
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