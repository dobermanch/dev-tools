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
                _args.Time,
                _args.Hyphens,
                _args.Case,
                _args.Brackets
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

    private Task OnHyphensValueChangedAsync(bool value)
    {
        _args.Hyphens = value;
        return OnValueChangedAsync();
    }

    private Task OnCaseValueChangedAsync(UuidGeneratorTool.UuidCase value)
    {
        _args.Case = value;
        return OnValueChangedAsync();
    }

    private Task OnBracketsValueChangedAsync(UuidGeneratorTool.UuidBrackets value)
    {
        _args.Brackets = value;
        return OnValueChangedAsync();
    }
    
    record Args
    {
        public UuidGeneratorTool.UuidType Type { get; set; } = UuidGeneratorTool.UuidType.V4;
        public int Count { get; set; } = 1;
        public Guid? Namespace { get; set; }
        public string? Name { get; set; }
        public DateTime? Time { get; set; }
        public bool Hyphens { get; set; } = true;
        public UuidGeneratorTool.UuidCase Case { get; set; } = UuidGeneratorTool.UuidCase.Lowercase;
        public UuidGeneratorTool.UuidBrackets Brackets { get; set; } = UuidGeneratorTool.UuidBrackets.None;
    }
}