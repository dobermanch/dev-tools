using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Components.Layout;

public partial class DtToolPageHeader : ComponentBase
{
    private IStringLocalizer _localizer = null!;
    private bool _shared;

    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter]
    public ToolDefinition ToolDefinition { get; set; } = null!;

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.CreateScopedLocalizer("Component.DtToolPageHeader");
        base.OnInitialized();
    }

    private async Task ShareAsync()
    {
        // Phones and Safari open the system share sheet; everywhere else the link goes to the clipboard,
        // and the icon ticks to say so.
        var outcome = await Context.JsService.ShareAsync(
            Context.Localization.GetToolTitle(ToolDefinition),
            Context.Navigation.Uri);

        if (outcome is not "copied")
        {
            return;
        }

        _shared = true;
        StateHasChanged();
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        _shared = false;
    }
}
