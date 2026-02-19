using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class UuidGeneratorToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private UuidGeneratorTool _tool = null!;
    private readonly Args _args = new();
    private UuidGeneratorTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<UuidGeneratorToolPage>();
        _tool = Context.ToolsProvider.GetTool<UuidGeneratorTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<UuidGeneratorTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        _result = await _tool.RunAsync(new UuidGeneratorTool.Args(
                _args.Type,
                _args.Count,
                _args.Namespace,
                _args.Name,
                _args.Time
            ),
            CancellationToken.None);
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
    
    record Args
    {
        public UuidGeneratorTool.UuidType Type { get; set; } = UuidGeneratorTool.UuidType.Nil;
        public int Count { get; set; } = 1;
        public Guid? Namespace { get; set; }
        public string? Name { get; set; }
        public DateTime? Time { get; set; }
    }
}