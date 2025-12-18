using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dev.Tools.Web.Components.Tools;

public partial class ToolCard : ComponentBase
{
    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter]
    public ToolDefinition Tool { get; set; } = null!;

    private void NavigateToToolPage()
    {
        Context.Navigation.NavigateTo($"/tools/{Tool.Name}");
    }

    private string GetToolIcon()
    {
        return Tool.Name switch
        {
            "base64-encoder" => Icons.Material.Outlined.DataObject,
            "base64-decoder" => Icons.Material.Outlined.DataArray,
            "case-converter" => Icons.Material.Outlined.TextFormat,
            "char-viewer" => Icons.Material.Outlined.Visibility,
            "date-convert" => Icons.Material.Outlined.Schedule,
            "hash" => Icons.Material.Outlined.Tag,
            "int-base-converter" => Icons.Material.Outlined.Calculate,
            "roman-converter" => Icons.Material.Outlined.FormatListNumbered,
            "ip-details" => Icons.Material.Outlined.Lan,
            "json-formatter" => Icons.Material.Outlined.Code,
            "passphrase-generator" => Icons.Material.Outlined.VpnKey,
            "rsa-generator" => Icons.Material.Outlined.Key,
            "url-transcoder" => Icons.Material.Outlined.Link,
            "nato" => Icons.Material.Outlined.RecordVoiceOver,
            "token-generator" => Icons.Material.Outlined.Token,
            "ulid-generator" => Icons.Material.Outlined.Fingerprint,
            "url-parser" => Icons.Material.Outlined.TravelExplore,
            "uuid-generator" => Icons.Material.Outlined.QrCode2,
            _ => Icons.Material.Outlined.Construction
        };
    }
}