using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtInputPane : ComponentBase, IDisposable
{
    private IStringLocalizer _localizer = null!;
    private string? _text;
    private string? _lastKnownValue;
    private string? _fileError;
    private string? _loadedFile;
    private CancellationTokenSource? _debounce;

    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter, EditorRequired]
    public string Label { get; set; } = null!;

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Fires once typing settles, the way a tool wants to be re-run.</summary>
    [Parameter]
    public EventCallback<string> OnDebounceIntervalElapsed { get; set; }

    [Parameter]
    public double DebounceInterval { get; set; } = 500;

    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// How tall the box starts. A value someone pastes wants the full height; a URL or a token needs a
    /// line or two, and a value that is one line by nature belongs in the options grid instead.
    /// </summary>
    [Parameter]
    public int MinHeight { get; set; } = 300;

    [Parameter]
    public int MaxLength { get; set; } = 524288;

    [Parameter]
    public bool AllowFileInput { get; set; }

    /// <summary>For a tool that takes <see cref="ToolContent"/>: the file arrives as bytes, not as text.</summary>
    [Parameter]
    public EventCallback<LoadedFile> OnFileLoaded { get; set; }

    [Parameter]
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

    [Parameter]
    public string? Class { get; set; }

    private int CharCount => _text?.Length ?? 0;

    private int LineCount => string.IsNullOrEmpty(_text) ? 0 : _text.Count(c => c == '\n') + 1;

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.CreateScopedLocalizer("Component.DtPane");
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // Follow the value when the page changes it, but leave what is being typed alone.
        if (_text is null || !string.Equals(Value, _lastKnownValue, StringComparison.Ordinal))
        {
            _lastKnownValue = Value;
            _text = Value ?? string.Empty;
        }

        base.OnParametersSet();
    }

    private async Task OnInputAsync(ChangeEventArgs args)
    {
        var text = args.Value as string ?? string.Empty;
        _text = text;
        _lastKnownValue = text;
        await ValueChanged.InvokeAsync(text);
        await DebounceAsync(text);
    }

    private async Task ClearAsync()
    {
        _text = string.Empty;
        _lastKnownValue = string.Empty;
        _fileError = null;
        _loadedFile = null;
        await ValueChanged.InvokeAsync(string.Empty);
        await OnDebounceIntervalElapsed.InvokeAsync(string.Empty);
    }

    private async Task DebounceAsync(string text)
    {
        var previous = _debounce;
        if (previous is not null)
        {
            await previous.CancelAsync();
            previous.Dispose();
        }

        var cancellation = new CancellationTokenSource();
        _debounce = cancellation;

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(DebounceInterval), cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Typing replaces whatever a file supplied.
        _fileError = null;
        _loadedFile = null;
        await OnDebounceIntervalElapsed.InvokeAsync(text);
    }

    private async Task OnFileSelectedAsync(IBrowserFile? file)
    {
        if (file is null)
        {
            return;
        }

        var asContent = OnFileLoaded.HasDelegate;
        var limit = asContent ? MaxFileSize : MaxLength;
        if (file.Size > limit)
        {
            _fileError = _localizer["Error.FileTooLarge", FileInput.FormatSize(limit)];
            return;
        }

        var content = await FileInput.ReadAsync(file, limit);

        _fileError = null;
        _loadedFile = _localizer["Text.LoadedFile", file.Name];

        if (asContent)
        {
            await OnFileLoaded.InvokeAsync(new LoadedFile(file.Name, content));
            return;
        }

        var text = await content.ReadAllTextAsync(CancellationToken.None);
        _text = text;
        _lastKnownValue = text;
        await ValueChanged.InvokeAsync(text);
        await OnDebounceIntervalElapsed.InvokeAsync(text);
    }

    public void Dispose()
    {
        _debounce?.Dispose();
    }
}
