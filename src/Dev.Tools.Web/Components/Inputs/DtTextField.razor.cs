using System.Globalization;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtTextField<T> : ComponentBase, IDisposable
{
    private readonly string _id = $"dt-input-{Guid.NewGuid():N}";

    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string Label { get; set; } = null!;

    [Parameter]
    public T? Value { get; set; }

    [Parameter]
    public EventCallback<T> ValueChanged { get; set; }

    [Parameter]
    public string? ErrorText { get; set; }

    [Parameter]
    public bool Error { get; set; }

    /// <summary>Guidance under the field, shown while there is no error.</summary>
    [Parameter]
    public string? Hint { get; set; }

    [Parameter]
    public double DebounceInterval { get; set; } = 500;

    [Parameter]
    public EventCallback<string> OnDebounceIntervalElapsed { get; set; }

    [Parameter]
    public int MaxLength { get; set; } = 524288;

    [Parameter]
    public bool ShowCopyButton { get; set; } = true;

    /// <summary>Only a date carries one; <see cref="BindConverter"/>'s format overloads are not generic.</summary>
    [Parameter]
    public string? Format { get; set; }

    [Parameter]
    public InputType InputType { get; set; } = InputType.Text;

    // What the input shows. Kept apart from Value so that typing is never undone by a re-render, and so
    // that a value the field cannot parse leaves the last good one in place.
    private string? _text;
    private T? _lastKnownValue;
    private CancellationTokenSource? _debounce;

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

    protected override void OnParametersSet()
    {
        // Follow the value when the page changes it, but leave what is being typed alone.
        if (_text is null || !EqualityComparer<T?>.Default.Equals(Value, _lastKnownValue))
        {
            _lastKnownValue = Value;
            _text = FormatValue(Value);
        }

        base.OnParametersSet();
    }

    private string? FormatValue(T? value)
        => value switch
        {
            null => string.Empty,
            IFormattable formattable when !string.IsNullOrEmpty(Format) => formattable.ToString(Format, CultureInfo.InvariantCulture),
            _ => BindConverter.FormatValue(value, CultureInfo.InvariantCulture)?.ToString()
        };

    private async Task OnInputAsync(ChangeEventArgs args)
    {
        var text = args.Value as string ?? string.Empty;
        _text = text;

        // A half-typed number is not a value: the last good one stays until this parses.
        if (BindConverter.TryConvertTo<T>(text, CultureInfo.InvariantCulture, out var parsed))
        {
            _lastKnownValue = parsed;
            await ValueChanged.InvokeAsync(parsed);
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

        await OnDebounceIntervalElapsed.InvokeAsync(text);
    }

    public void Dispose()
    {
        _debounce?.Dispose();
    }
}
