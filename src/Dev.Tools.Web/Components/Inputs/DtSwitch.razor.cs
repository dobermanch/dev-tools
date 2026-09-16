using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtSwitch : ComponentBase
{
    private readonly string _id = $"dt-switch-{Guid.NewGuid():N}";

    private IStringLocalizer _localizer = null!;

    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public bool Value { get; set; }

    [Parameter]
    public EventCallback<bool> ValueChanged { get; set; }

    [Parameter]
    public string? Hint { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? Class { get; set; }

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.CreateScopedLocalizer("Component.DtSwitch");
        base.OnInitialized();
    }

    private async Task ToggleAsync()
    {
        if (Disabled)
        {
            return;
        }

        await ValueChanged.InvokeAsync(!Value);
    }
}
