using Dev.Tools.Web.Components.Inputs;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class HashTextToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private HashTextTool _tool = null!;
    private readonly Args _args = new();
    private Dictionary<HashTextTool.HashAlgorithm, HashTextTool.Result?> _results = new();
    private IStringLocalizer _localizer = null!;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<HashTextToolPage>();
        _tool = Context.ToolsProvider.GetTool<HashTextTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<HashTextTool>();
        _results = Enum.GetValues<HashTextTool.HashAlgorithm>()
            .ToDictionary(it => it, it => (HashTextTool.Result?)null);

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync(string value)
    {
        _args.Text = value;
        await HashAsync(value);
    }

    // A file is hashed as its bytes, not as text; the field empties, and says which file it was.
    private async Task OnFileLoadedAsync(LoadedFile file)
    {
        _args.Text = string.Empty;
        await HashAsync(file.Content);
    }

    private async Task HashAsync(ToolContent content)
    {
        var tasks = new List<Task>();
        foreach (var algorithm in _results.Keys)
        {
            tasks.Add(_tool.RunAsync(new HashTextTool.Args
                    (
                        Text: content,
                        Algorithm: algorithm
                    ),
                    CancellationToken.None)
                .ContinueWith(it =>
                {
                    _results[algorithm] = it.Result;
                }));
        }

        await Task.WhenAll(tasks);
    }

    private record Args
    {
        public string? Text { get; set; }
        public HashTextTool.HashAlgorithm Algorithm { get; set; } = HashTextTool.HashAlgorithm.Md5;
    }
}
