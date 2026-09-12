namespace Dev.Tools;

/// <summary>
/// Broad grouping a tool belongs to. Members carry a <see cref="DescriptionAttribute"/> only where
/// the English label differs from the member name; otherwise the member name is the label.
/// </summary>
public enum Category
{
    Converter,

    [Description("Cryptography")]
    Crypto,

    Security,
    Text,
    Network,
    Web
}
