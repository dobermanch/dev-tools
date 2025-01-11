namespace Dev.Tools;

[GenerateValues]
public partial record struct ErrorCode(string Value)
{
    public const string Unknown = nameof(Unknown);
    public const string NamespaceEmpty = nameof(NamespaceEmpty);
    public const string TextEmpty = nameof(TextEmpty);
    public const string FailedToDecrypt = nameof(FailedToDecrypt);
    public const string InputNotValid = nameof(InputNotValid);
    
    public static implicit operator string(ErrorCode code) => code.Value;
    
    public static implicit operator ErrorCode(string code) => new (code);
}