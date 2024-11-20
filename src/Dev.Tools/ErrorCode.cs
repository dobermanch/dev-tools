namespace Dev.Tools;

[GenerateValues]
public partial record struct ErrorCode(string Value)
{
    public const string Unknown = nameof(Unknown);
    public const string NamespaceEmpty = nameof(Unknown);
    public const string TextEmpty = nameof(Unknown);
    
    public static implicit operator string(ErrorCode code) => code.Value;
    
    public static implicit operator ErrorCode(string code) => new (code);
}