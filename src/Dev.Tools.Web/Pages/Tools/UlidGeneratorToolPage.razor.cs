using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class UlidGeneratorToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private UlidGeneratorTool _tool = null!;
    private readonly Args _args = new();
    private UlidGeneratorTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<UlidGeneratorToolPage>();
        _tool = Context.ToolsProvider.GetTool<UlidGeneratorTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<UlidGeneratorTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        _result = await _tool.RunAsync(new UlidGeneratorTool.Args(
                _args.Type,
                _args.Count),
            CancellationToken.None);
    }

    private string GetUlids()
    {
        return _result != null ? string.Join(Environment.NewLine, _result.Data) : string.Empty;
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
    
    record Args
    {
        public UlidGeneratorTool.UlidType Type { get; set; } = UlidGeneratorTool.UlidType.Random;
        public int Count { get; set; } = 1;
    }
}