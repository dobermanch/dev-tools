using System.Globalization;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Components.Layout;

public partial class DtTable<T> : ComponentBase
{
    private IStringLocalizer _localizer = null!;
    private IReadOnlyList<T> _items = [];

    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter]
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>The header row: plain <c>&lt;th&gt;</c> cells.</summary>
    [Parameter, EditorRequired]
    public RenderFragment HeaderContent { get; set; } = null!;

    /// <summary>One row: plain <c>&lt;td&gt;</c> cells.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<T> RowTemplate { get; set; } = null!;

    /// <summary>Shown above the table, with the row count beside it.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Caps the height in pixels and scrolls inside, keeping the header in view.</summary>
    [Parameter]
    public int? MaxHeight { get; set; }

    [Parameter]
    public string? Class { get; set; }

    private string? MaxHeightStyle => MaxHeight is null
        ? null
        : $"max-height: {MaxHeight.Value.ToString(CultureInfo.InvariantCulture)}px";

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.CreateScopedLocalizer("Component.DtTable");
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // Counted and walked more than once below, so it is enumerated exactly once here.
        _items = Items as IReadOnlyList<T> ?? Items.ToList();
        base.OnParametersSet();
    }
}
