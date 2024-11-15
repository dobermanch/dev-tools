namespace Dev.Tools;

public partial record struct Keyword(string Value)
{
    public const string Base64 = nameof(Base64);
    public const string Convert = nameof(Convert);
    public const string Decode = nameof(Decode);
    public const string Encode = nameof(Encode);
    public const string Generate = nameof(Generate);
    public const string Guid = nameof(Guid);
    public const string Hash = nameof(Hash);
    public const string Misc = nameof(Misc);
    public const string String = nameof(String);
    public const string Password = nameof(Password);
    public const string Text = nameof(Text);
    public const string Url = nameof(Url);
    public const string Uuid = nameof(Uuid);

    public static implicit operator string(Keyword keyword) => keyword.Value;
    
    public static explicit operator Keyword(string keyword) => new(keyword);
}