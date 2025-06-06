namespace Dev.Tools;

[GenerateValues]
public partial record struct Keyword(string Value)
{
    public const string Base64 = nameof(Base64);
    public const string Convert = nameof(Convert);
    public const string Case = nameof(Case);
    public const string Decode = nameof(Decode);
    public const string Encode = nameof(Encode);
    public const string Generate = nameof(Generate);
    public const string Guid = nameof(Guid);
    public const string Hash = nameof(Hash);
    public const string Misc = nameof(Misc);
    public const string String = nameof(String);
    public const string Json = nameof(Json);
    public const string Xml = nameof(Xml);
    public const string Format = nameof(Format);
    public const string Password = nameof(Password);
    public const string Text = nameof(Text);
    public const string Url = nameof(Url);
    public const string Uuid = nameof(Uuid);
    public const string Network = nameof(Network);
    public const string Ip = nameof(Ip);
    public const string Internet = nameof(Internet);
    public const string Token = nameof(Token);

    public static implicit operator string(Keyword keyword) => keyword.Value;
    
    public static explicit operator Keyword(string keyword) => new(keyword);
}