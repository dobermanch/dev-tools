namespace Dev.Tools;

[GenerateValues]
public partial record struct Category(string Value)
{
    public const string None = nameof(None);
    public const string Converter = nameof(Converter);
    public const string Crypto = nameof(Crypto);
    public const string Misc = nameof(Misc);
    public const string Security = nameof(Security);
    public const string Text = nameof(Text);
    public const string Network = nameof(Network);
    
    public static implicit operator string(Category category) => category.Value;

    public static explicit operator Category(string category) => new(category);
}