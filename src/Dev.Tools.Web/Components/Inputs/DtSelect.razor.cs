using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Dev.Tools.Web.Components.Inputs;

/// <summary>
/// A select whose list the app draws itself, shared by every select in the UI. The value is
/// whatever the caller holds - an enum member, a culture - and <see cref="ItemToDisplay"/> turns
/// it into the label, so the component never knows what it is showing.
/// </summary>
public partial class DtSelect<TValue> : ComponentBase
{
    private readonly string _id = $"dt-select-{Guid.NewGuid():N}";

    private bool _open;

    /// <summary>The option the keyboard is on, which is not yet the chosen one.</summary>
    private int _highlighted;

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public TValue? Value { get; set; }

    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    [Parameter, EditorRequired]
    public IReadOnlyList<TValue> Items { get; set; } = [];

    /// <summary>The label for an option; its <c>ToString</c> when no other is given.</summary>
    [Parameter]
    public Func<TValue, string>? ItemToDisplay { get; set; }

    [Parameter]
    public string? Hint { get; set; }

    [Parameter]
    public bool Error { get; set; }

    [Parameter]
    public string? ErrorText { get; set; }

    [Parameter]
    public string? Class { get; set; }

    private string OptionId(int index) => $"{_id}-option-{index}";

    private string Display(TValue? value) => value is null
        ? string.Empty
        : ItemToDisplay?.Invoke(value) ?? value.ToString() ?? string.Empty;

    private bool IsSelected(TValue item) => EqualityComparer<TValue>.Default.Equals(item, Value);

    private void Toggle()
    {
        if (_open)
        {
            _open = false;
            return;
        }

        Open();
    }

    /// <summary>Opens with the keyboard already on the chosen option, so arrows move from there.</summary>
    private void Open()
    {
        _open = true;
        _highlighted = Math.Max(IndexOfValue(), 0);
    }

    private void Close() => _open = false;

    private int IndexOfValue()
    {
        for (var index = 0; index < Items.Count; index++)
        {
            if (IsSelected(Items[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private async Task SelectAsync(TValue item)
    {
        _open = false;

        if (!IsSelected(item))
        {
            await ValueChanged.InvokeAsync(item);
        }
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs args)
    {
        switch (args.Key)
        {
            case "Escape":
                Close();
                break;

            case "ArrowDown":
                Step(1);
                break;

            case "ArrowUp":
                Step(-1);
                break;

            case "Home" when _open:
                _highlighted = 0;
                break;

            case "End" when _open:
                _highlighted = Items.Count - 1;
                break;

            case "Enter" when _open && _highlighted >= 0 && _highlighted < Items.Count:
                await SelectAsync(Items[_highlighted]);
                break;
        }
    }

    /// <summary>An arrow opens the closed list rather than changing the value behind it.</summary>
    private void Step(int delta)
    {
        if (!_open)
        {
            Open();
            return;
        }

        if (Items.Count > 0)
        {
            _highlighted = (_highlighted + delta + Items.Count) % Items.Count;
        }
    }
}
