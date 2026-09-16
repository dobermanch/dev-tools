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

    // What the results were computed from - typed text or a loaded file - and the HMAC key, if any.
    private ToolContent? _content;
    private string? _key;

    [Inject] private WebContext Context { get; set; } = null!;

    // What copy and download act on: every digest, labelled, in one block.
    private string AllDigests => string.Join(
        Environment.NewLine,
        _results.Where(it => !string.IsNullOrEmpty(it.Value?.Data))
            .Select(it => $"{Context.Localization.GetToolEnum(_toolDefinition, it.Key)}: {it.Value!.Data}"));

    private string? FirstErrorText => _results.Values
        .Where(it => it?.HasErrors ?? false)
        .SelectMany(it => it!.Errors)
        .Select(it => Context.Localization.GetErrorCode(it.Code))
        .FirstOrDefault();

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
        _content = value;
        await HashAsync(_content);
    }

    // A file is hashed as its bytes, not as text; the field empties, and says which file it was.
    private async Task OnFileLoadedAsync(LoadedFile file)
    {
        _args.Text = string.Empty;
        _content = file.Content;
        await HashAsync(_content);
    }

    // The loaded file holds its bytes, so a new key hashes the same content again.
    private async Task OnKeyChangedAsync(string value)
    {
        _key = value;
        if (_content is not null)
        {
            await HashAsync(_content);
        }
    }

    private async Task HashAsync(ToolContent content)
    {
        var tasks = new List<Task>();
        foreach (var algorithm in _results.Keys)
        {
            tasks.Add(_tool.RunAsync(new HashTextTool.Args
                    (
                        Text: content,
                        Algorithm: algorithm,
                        Key: _key
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
