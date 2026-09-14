using Dev.Tools.Web.Components.Inputs;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class BaseEncodingToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private BaseEncodingTool _tool = null!;
    private BaseEncodingTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private BaseEncodingTool.TranscodingType _transcoding = BaseEncodingTool.TranscodingType.Encode;
    private bool _urlSafe;
    private bool _insertLineBreaks;
    private string _text = string.Empty;

    // What a loaded file supplied, until the user types again. Kept, so changing an option runs it again.
    private ToolContent? _file;

    [Inject] private WebContext Context { get; set; } = null!;

    private string? InputError => _result?.Errors
        .Select(it => Context.Localization.GetErrorCode(it.Code))
        .FirstOrDefault();

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.PageLocalizer<BaseEncodingToolPage>();
        _tool = Context.ToolsProvider.GetTool<BaseEncodingTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<BaseEncodingTool>();

        base.OnInitialized();
    }

    private async Task OnTextChangedAsync(string value)
    {
        _text = value;
        _file = null;
        await RunAsync();
    }

    // A file is encoded as its bytes, not as text; the field empties, and says which file it was.
    private async Task OnFileLoadedAsync(LoadedFile file)
    {
        _text = string.Empty;
        _file = file.Content;
        await RunAsync();
    }

    private async Task SetOptionAsync(Action set)
    {
        set();
        await RunAsync();
    }

    private async Task RunAsync()
    {
        if (_file is null && string.IsNullOrEmpty(_text))
        {
            _result = null;
            return;
        }

        _result = await _tool.RunAsync(
            new BaseEncodingTool.Args(_file ?? _text, _transcoding, _urlSafe, _insertLineBreaks),
            CancellationToken.None);
    }
}
