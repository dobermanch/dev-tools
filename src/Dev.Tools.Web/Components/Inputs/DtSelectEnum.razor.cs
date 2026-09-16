using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtSelectEnum<T> : ComponentBase
    where T: struct, Enum
{
    private static readonly T[] EnumValues = Enum.GetValues<T>();

    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter]
    public string Label { get; set; } = null!;

    [Parameter]
    public T Value { get; set; }

    [Parameter]
    public EventCallback<T> ValueChanged { get; set; }

    [Parameter]
    public string? Hint { get; set; }

    [Parameter]
    public bool Error { get; set; }

    [Parameter]
    public string? ErrorText { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public Func<T, string>? ValueToDisplay { get; set; }

    private string Display(T value) => ValueToDisplay?.Invoke(value) ?? Context.Localization.GetEnum(value);
}
