using MudBlazor;

namespace Dev.Tools.Web.Services;

public static class ToolIconProvider
{
    public static string GetToolIcon(ToolDefinition tool) => GetToolIcon(tool.Name);

    public static string GetToolIcon(string toolName)
    {
        return toolName switch
        {
            "base-encoding" => Icons.Material.Outlined.DataObject,
            "case-converter" => Icons.Material.Outlined.TextFormat,
            "char-viewer" => Icons.Material.Outlined.Visibility,
            "date-convert" => Icons.Material.Outlined.Schedule,
            "hash" => Icons.Material.Outlined.Tag,
            "int-base-converter" => Icons.Material.Outlined.Calculate,
            "roman-converter" => Icons.Material.Outlined.FormatListNumbered,
            "ip-details" => Icons.Material.Outlined.Lan,
            "json-formatter" => Icons.Material.Outlined.Code,
            "passphrase-generator" => Icons.Material.Outlined.VpnKey,
            "url-transcoder" => Icons.Material.Outlined.Link,
            "nato" => Icons.Material.Outlined.RecordVoiceOver,
            "token-generator" => Icons.Material.Outlined.Token,
            "ulid-generator" => Icons.Material.Outlined.Fingerprint,
            "url-parser" => Icons.Material.Outlined.TravelExplore,
            "uuid-generator" => Icons.Material.Outlined.QrCode2,
            "jwt-parser" => Icons.Material.Outlined.Badge,
            "lorem-ipsum" => Icons.Material.Outlined.Article,
            "encryption" => Icons.Material.Outlined.Lock,
            "text-statistics" => Icons.Material.Outlined.Analytics,
            "html-entities" => Icons.Material.Outlined.Html,
            "data-converter" => Icons.Material.Outlined.Transform,
            "regex-tester" => Icons.Material.Outlined.ManageSearch,
            "cron-parser" => Icons.Material.Outlined.EventRepeat,
            "ip-subnet" => Icons.Material.Outlined.SettingsEthernet,
            "ip-converter" => Icons.Material.Outlined.Numbers,
            "otp" => Icons.Material.Outlined.PhonelinkLock,
            "text-diff" => Icons.Material.Outlined.Difference,
            "bcrypt" => Icons.Material.Outlined.Password,
            "color-converter" => Icons.Material.Outlined.Palette,
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
            Category.Development => Icons.Material.Outlined.DeveloperMode,
            _ => Icons.Material.Outlined.Category
        };
    }
}
