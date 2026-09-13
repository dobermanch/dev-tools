using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtTextField<T> : ComponentBase
{
    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter]
    public string Class { get; set; } = null!;

    [Parameter]
    public string Label { get; set; } = null!;

    [Parameter]
    public LabelPositionType LabelPosition { get; set; }

    [Parameter]
    public T? Value { get; set; }

    [Parameter]
    public EventCallback<T> ValueChanged { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public string? ErrorText { get; set; }

    [Parameter]
    public bool Error { get; set; }

    [Parameter]
    public double DebounceInterval { get; set; } = 500;

    [Parameter]
    public EventCallback<string> OnDebounceIntervalElapsed { get; set; }

    [Parameter]
    public int MaxLength { get; set; } = 524288;

    [Parameter]
    public bool ShowCopyButton { get; set; } = true;

    [Parameter]
    public string? Format { get; set; }

    [Parameter]
    public InputType InputType { get; set; } = InputType.Text;

    [Parameter]
    public int Lines { get; set; } = 1;

    [Parameter]
    public Func<T, bool>? Validation { get; set; }

    /// <summary>
    /// Offers loading the value from a file. A tool does not care where its input came from, so this is
    /// the field's business, not the page's: the file is read as text and arrives the way typed text
    /// does, through <see cref="ValueChanged"/> and <see cref="OnDebounceIntervalElapsed"/>, up to
    /// <see cref="MaxLength"/> bytes.
    /// </summary>
    [Parameter]
    public bool AllowFileInput { get; set; }

    /// <summary>
    /// For a tool that takes <see cref="ToolContent"/>: the file arrives as it is, bytes and all, instead
    /// of as text, up to <see cref="MaxFileSize"/>.
    /// </summary>
    [Parameter]
    public EventCallback<LoadedFile> OnFileLoaded { get; set; }

    [Parameter]
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

    private IStringLocalizer _localizer = null!;
    private string? _fileError;
    private string? _loadedFile;

    protected override void OnInitialized()
    {
        // Scoped by name rather than through ComponentLocalizer<T>: the generic type's name is DtTextField`1.
        _localizer = Context.Localization.CreateScopedLocalizer("Component.DtTextField");
        base.OnInitialized();
    }

    private async Task OnTextChangedAsync(string text)
    {
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
            _fileError = _localizer["Error.FileTooLarge", FormatSize(limit)];
            return;
        }

        // The browser hands a file over once. Holding its bytes lets a page run the tool as often as it
        // likes - again when an option changes, or once per algorithm.
        await using var stream = file.OpenReadStream(limit);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        var content = ToolContent.FromBytes(buffer.ToArray());

        _fileError = null;
        _loadedFile = _localizer["Text.LoadedFile", file.Name];

        if (asContent)
        {
            await OnFileLoaded.InvokeAsync(new LoadedFile(file.Name, content));
            return;
        }

        var text = await content.ReadAllTextAsync(CancellationToken.None);
        if (text is T value)
        {
            await ValueChanged.InvokeAsync(value);
        }

        await OnDebounceIntervalElapsed.InvokeAsync(text);
    }

    private static string FormatSize(long bytes)
        => bytes >= 1024 * 1024 ? $"{bytes / (1024 * 1024)} MB" : $"{bytes / 1024} KB";

    private async Task OnCopyToClipboardAsync()
    {
        if (Value is not null && !string.IsNullOrEmpty(Value.ToString()))
        {
            await Context.JsService.CopyToClipboardAsync(Value.ToString()!);
        }
    }
}
