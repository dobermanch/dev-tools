using Dev.Tools.Providers;
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

    [Inject] 
    private IToolsProvider Provider { get; set; } = null!;
    
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;
    
    [Inject]
    private IJsServices JsServices { get; set; } = null!;
    
    protected override async Task OnInitializedAsync()
    {
        _tool = Provider.GetTool<HashTextTool>();
        _toolDefinition = Provider.GetToolDefinition<HashTextTool>();
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

    private void NavigateToPreviousPage()
    {
        Navigation.NavigateTo("/");
    }

    private async Task OnCopyToClipboardAsync(string? textToCopy)
    {
        if (!string.IsNullOrEmpty(textToCopy))
        {
            await JsServices.CopyToClipboardAsync(textToCopy);
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