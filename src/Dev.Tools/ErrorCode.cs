namespace Dev.Tools;

/// <summary>
/// Culture-neutral failure reason returned by a tool. The English message on each member is what
/// CLI, API and MCP surface; the Web frontend translates the member name instead.
/// </summary>
public enum ErrorCode
{
    [Description("Unknown error")]
    Unknown,

    [Description("Namespace cannot be empty")]
    NamespaceEmpty,

    [Description("Text cannot be empty")]
    TextEmpty,

    [Description("Failed to decrypt")]
    FailedToDecrypt,

    [Description("Input is not valid")]
    InputNotValid,

    [Description("Invalid number base")]
    WrongBase,

    [Description("Invalid format")]
    WrongFormat,

    // Appended rather than placed beside FailedToDecrypt, so the numeric values of existing members hold.
    [Description("Failed to encrypt")]
    FailedToEncrypt
}
