using Dev.Tools.Web.Core.Messaging;
using Dev.Tools.Web.Notifications;
using Dev.Tools.Web.Services;
using Dev.Tools.Web.Services.Preferences;
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

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.CreateScopedLocalizer("Component.DtToolPageHeader");
        await RecordVisitAsync();
        await base.OnInitializedAsync();
    }

    /// <summary>
    /// Every tool page goes through this header, so opening one is recorded here rather than in each of
    /// them. Most recent first, and opening a tool again moves it back to the front.
    /// </summary>
    private async Task RecordVisitAsync()
    {
        var tools = Context.Preferences.Preferences.Recent.Tools;
        if (tools.Count > 0 && string.Equals(tools[0], ToolDefinition.Name, StringComparison.Ordinal))
        {
            return;
        }

        var recent = new List<string> { ToolDefinition.Name };
        recent.AddRange(tools
            .Where(name => !string.Equals(name, ToolDefinition.Name, StringComparison.Ordinal))
            .Take(UserPreferences.RecentDetails.Capacity - 1));

        await Context.Preferences.UpdateRecentAsync(new UserPreferences.RecentDetails { Tools = recent });
        await Context.Messenger.Publish(new RecentChangedNotification(ToolDefinition.Name));
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
