using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class CaseConverterPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private CaseConverterTool _tool = null!;
    private readonly Args _args = new();
    private Dictionary<CaseConverterTool.CaseType, CaseConverterTool.Result?> _results = new();

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
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
            tasks.Add(_tool.RunAsync(new CaseConverterTool.Args()
                    {
                        Text = _args.Text,
                        Type = caseType
                    },
                    CancellationToken.None)
                .ContinueWith(it =>
                {
                    _results[caseType] = it.Result;
                }));
        }
        
        await Task.WhenAll(tasks);
    }

    private string ErrorMessage()
    {
        return string.Join(
            "; ",
            _results
                .Where(it => it.Value?.HasErrors ?? false)
                .Select(it => $"{it.Key.ToString()}: {it.Value!.ErrorCodes[0]}")
        );
    }
    
    public record Args
    {
        public string Text { get; set; } = null!;
        public CaseConverterTool.CaseType Type { get; set; }
    }
}