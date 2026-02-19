using MudBlazor;

namespace Dev.Tools.Web.Services;

public static class ToolIconProvider
{
    public static string GetToolIcon(ToolDefinition tool) => GetToolIcon(tool.Name);

    public static string GetToolIcon(string toolName)
    {
        return toolName switch
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
            "jwt-parser" => Icons.Material.Outlined.Badge,
            "lorem-ipsum" => Icons.Material.Outlined.Article,
            _ => Icons.Material.Outlined.Construction
        };
    }

    public static string GetCategoryIcon(Category category)
    {
        return category switch
        {
            Category.Converter => Icons.Material.Outlined.SwapHoriz,
            Category.Crypto => Icons.Material.Outlined.EnhancedEncryption,
            Category.Security => Icons.Material.Outlined.Security,
            Category.Text => Icons.Material.Outlined.TextFields,
            Category.Network => Icons.Material.Outlined.Lan,
            Category.Web => Icons.Material.Outlined.Language,
            _ => Icons.Material.Outlined.Category
        };
    }
}
