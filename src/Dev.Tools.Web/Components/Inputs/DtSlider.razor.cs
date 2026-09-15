using System.Globalization;
using System.Numerics;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtSlider<T> : ComponentBase
    where T : struct, INumber<T>
{
    private readonly string _id = $"dt-slider-{Guid.NewGuid():N}";

    // The label follows the handle while it is dragged; the tool only re-runs once it is let go.
    private T _displayValue;

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string Label { get; set; } = null!;

    [Parameter]
    public T MinValue { get; set; }

    [Parameter]
    public T MaxValue { get; set; }

    [Parameter]
    public T Step { get; set; } = T.One;

    [Parameter]
    public T Value { get; set; }

    [Parameter]
    public EventCallback<T> ValueChanged { get; set; }

    [Parameter]
    public string? Hint { get; set; }

    private string LabelWithValue => $"{Label} — {_displayValue.ToString(null, CultureInfo.InvariantCulture)}";

    protected override void OnParametersSet()
    {
        _displayValue = Value;
        base.OnParametersSet();
    }

    private void OnInput(ChangeEventArgs args)
    {
        if (TryParse(args.Value, out var value))
        {
            _displayValue = value;
        }
    }

    private async Task OnChangedAsync(ChangeEventArgs args)
    {
        if (TryParse(args.Value, out var value))
        {
            _displayValue = value;
            await ValueChanged.InvokeAsync(value);
        }
    }

    private static bool TryParse(object? raw, out T value)
        => T.TryParse(raw as string, CultureInfo.InvariantCulture, out value);
}
