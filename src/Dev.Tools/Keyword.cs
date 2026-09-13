namespace Dev.Tools;

/// <summary>
/// Search keyword attached to a tool. Members carry a <see cref="DescriptionAttribute"/> only where
/// the English label differs from the member name; otherwise the member name is the label.
/// </summary>
public enum Keyword
{
    Base64,
    Convert,
    Case,
    Decode,
    Encode,
    Generate,

    [Description("GUID")]
    Guid,

    Hash,
    String,

    [Description("JSON")]
    Json,

    [Description("XML")]
    Xml,

    Format,
    Password,
    Text,

    [Description("URL")]
    Url,

    [Description("UUID")]
    Uuid,

    Network,

    [Description("IP Address")]
    Ip,

    Internet,
    Token,
    Web,

    [Description("JWT")]
    Jwt,

    [Description("Lorem Ipsum")]
    Lorem,

    Encrypt,

    [Description("HTML")]
    Html,

    [Description("HMAC")]
    Hmac
}
