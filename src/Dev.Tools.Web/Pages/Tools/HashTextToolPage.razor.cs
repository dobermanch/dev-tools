using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class HashTextToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition;
    private HashTextTool _tool = null!;
    private readonly HashTextTool.Args _args = new();
    private Dictionary<HashTextTool.HashAlgorithm, HashTextTool.Result?> _results = new();

    [Inject] private WebContext Context { get; set; } = null!;
    
    protected override async Task OnInitializedAsync()
    {
        _tool = Context.ToolsProvider.GetTool<HashTextTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<HashTextTool>();
        _results = Enum.GetValues<HashTextTool.HashAlgorithm>()
            .ToDictionary(it => it, it => (HashTextTool.Result?)null);

        await base.OnInitializedAsync();
    }
    
    private async Task OnValueChangedAsync(string value)
    {
        _args.Text = value;
        
        var tasks = new List<Task>();
        foreach (var algorithm in _results.Keys)
        {
            tasks.Add(_tool.RunAsync(new HashTextTool.Args()
                    {
                        Text = value,
                        Algorithm = algorithm
                    },
                    CancellationToken.None)
                .ContinueWith(it =>
                {
                    _results[algorithm] = it.Result;
                }));
        }
        
        await Task.WhenAll(tasks);
    }
    
    private async Task OnCopyToClipboardAsync(string? textToCopy)
    {
        if (!string.IsNullOrEmpty(textToCopy))
        {
            await Context.JsService.CopyToClipboardAsync(textToCopy);
        }
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
}