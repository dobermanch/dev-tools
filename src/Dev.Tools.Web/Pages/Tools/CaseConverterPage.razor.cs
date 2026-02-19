using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class CaseConverterPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private CaseConverterTool _tool = null!;
    private readonly Args _args = new();
    private Dictionary<CaseConverterTool.CaseType, CaseConverterTool.Result?> _results = new();
    private IStringLocalizer _localizer = null!;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<CaseConverterPage>();
        _tool = Context.ToolsProvider.GetTool<CaseConverterTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<CaseConverterTool>();
        _results = Enum
            .GetValues<CaseConverterTool.CaseType>()
            .ToDictionary(it => it, it => (CaseConverterTool.Result?)null);

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync(string value)
    {
        _args.Text = value;

        var tasks = new List<Task>();
        foreach (var caseType in _results.Keys)
        {
            tasks.Add(_tool.RunAsync(
                    new CaseConverterTool.Args(_args.Text, caseType),
                    CancellationToken.None)
                .ContinueWith(it =>
                {
                    _results[caseType] = it.Result;
                }));
        }

        await Task.WhenAll(tasks);
    }

    public record Args
    {
        public string Text { get; set; } = null!;
        public CaseConverterTool.CaseType Type { get; set; }
    }
}