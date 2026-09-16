using System.Globalization;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtTextField<T> : ComponentBase, IDisposable
{
    private readonly string _id = $"dt-input-{Guid.NewGuid():N}";

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

    /// <summary>
    /// Marks the field when the value is not acceptable. A value that cannot be judged - because the
    /// predicate throws on it, as an empty string does for a numeric one - counts as not acceptable.
    /// </summary>
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

    // What the input shows. Kept apart from Value so that typing is never undone by a re-render, and so
    // that a value the field cannot parse leaves the last good one in place.
    private string? _text;
    private T? _lastKnownValue;
    private bool _isValid = true;
    private CancellationTokenSource? _debounce;

    /// <summary>Multi-line fields still render through MudBlazor; B4 turns them into panes.</summary>
    private bool IsMultiline => Lines > 1;

    private bool HasError => Error || _fileError is not null || !_isValid;

    private string HtmlInputType => InputType switch
    {
        InputType.Number => "number",
        InputType.Password => "password",
        InputType.Email => "email",
        InputType.Telephone => "tel",
        InputType.Url => "url",
        InputType.Search => "search",
        InputType.Date => "date",
        InputType.DateTimeLocal => "datetime-local",
        InputType.Month => "month",
        InputType.Time => "time",
        InputType.Week => "week",
        InputType.Color => "color",
        _ => "text"
    };

    protected override void OnInitialized()
    {
        // Scoped by name rather than through ComponentLocalizer<T>: the generic type's name is DtTextField`1.
        _localizer = Context.Localization.CreateScopedLocalizer("Component.DtTextField");
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // Follow the value when the page changes it, but leave what is being typed alone.
        if (_text is null || !EqualityComparer<T?>.Default.Equals(Value, _lastKnownValue))
        {
            _lastKnownValue = Value;
            _text = FormatValue(Value);
            _isValid = Validate(Value);
        }

        base.OnParametersSet();
    }

    private string? FormatValue(T? value)
        => value switch
        {
            null => string.Empty,
            // A Format is only ever given for a date; BindConverter's own format overloads are not generic.
            IFormattable formattable when !string.IsNullOrEmpty(Format) => formattable.ToString(Format, CultureInfo.InvariantCulture),
            _ => BindConverter.FormatValue(value, CultureInfo.InvariantCulture)?.ToString()
        };

    private bool Validate(T? value)
    {
        if (Validation is null || value is null)
        {
            return true;
        }

        try
        {
            return Validation(value);
        }
        catch
        {
            // A predicate that throws on this value - int.Parse on "" - means it is not usable.
            return false;
        }
    }

    private async Task OnInputAsync(ChangeEventArgs args)
    {
        var text = args.Value as string ?? string.Empty;
        _text = text;

        if (BindConverter.TryConvertTo<T>(text, CultureInfo.InvariantCulture, out var parsed) && Validate(parsed))
        {
            _isValid = true;
            _lastKnownValue = parsed;
            await ValueChanged.InvokeAsync(parsed);
        }
        else
        {
            // Keep the last value the page was given: a half-typed number is not a value.
            _isValid = false;
        }

        await DebounceAsync(text);
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

        await OnTextChangedAsync(text);
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
        if (text is T value)
        {
            _text = text;
            _lastKnownValue = value;
            _isValid = Validate(value);
            await ValueChanged.InvokeAsync(value);
        }

        await OnDebounceIntervalElapsed.InvokeAsync(text);
    }

    private async Task OnCopyToClipboardAsync()
    {
        if (Value is not null && !string.IsNullOrEmpty(Value.ToString()))
        {
            await Context.JsService.CopyToClipboardAsync(Value.ToString()!);
        }
    }

    public void Dispose()
    {
        _debounce?.Dispose();
    }
}
