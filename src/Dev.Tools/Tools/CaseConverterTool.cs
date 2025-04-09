namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "case-converter",
    Aliases = ["case"],
    Keywords = [Keyword.Text, Keyword.String, Keyword.Convert, Keyword.Case],
    Categories = [Category.Converter, Category.Text],
    ErrorCodes = [ErrorCode.Unknown]
)]
public sealed class CaseConverterTool : ToolBase<CaseConverterTool.Args, CaseConverterTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (args.Text == null)
        {
            return new Result(string.Empty);
        }

        var text = args.Type switch
        {
            CaseType.LowerCase => args.Text.ToLowerInvariant().ToCharArray(),
            CaseType.UpperCase => args.Text.ToUpperInvariant().ToCharArray(),
            CaseType.CamelCase => ToCamelCase(args.Text, false),
            CaseType.CapitalCase => ToCapitalCase(args.Text, ' '),
            CaseType.ConstantCase => ToSeparatedString(args.Text, '_', true),
            CaseType.DotCase => ToSeparatedString(args.Text, '.', false),
            CaseType.HeaderCase => ToCapitalCase(args.Text, '-'),
            CaseType.PathForwardCase => ToSeparatedString(args.Text, '/', false),
            CaseType.NoCase => ToSeparatedString(args.Text, ' ', false),
            CaseType.ParamCase => ToSeparatedString(args.Text, '-', false),
            CaseType.PascalCase => ToCamelCase(args.Text, true),
            CaseType.SentenceCase => ToSentenceCase(args.Text),
            CaseType.SnakeCase => ToSeparatedString(args.Text, '_', false),
            CaseType.MockingCase => ToMockingCase(args.Text),
            CaseType.PathBackwardCase => ToSeparatedString(args.Text, '\\', false),
            _ => args.Text.ToCharArray()
        };
        
        return new Result(new string(text));
    }

    private static char[] ToCamelCase(string text, bool uppercase)
    {
        var index = 0;
        var nextIsUpper = false;
        var chars = new char[text.Length];
        foreach (var ch in text)
        {
            if (!char.IsLetterOrDigit(ch))
            {
                nextIsUpper = true;
                continue;
            }
        
            chars[index++] = nextIsUpper ? char.ToUpperInvariant(ch) : ch;
            nextIsUpper = false;
        }
        
        chars[0] = uppercase ? char.ToUpperInvariant(text[0]) : char.ToLowerInvariant(text[0]);
        
        return chars[..index];
    }
    
    private static char[] ToSeparatedString(string text, char separator, bool uppercase)
    {
        var nextSpace = false;
        var chars = new List<char>(text.Length);
        foreach (var ch in text)
        {
            if (!char.IsLetterOrDigit(ch))
            {
                nextSpace = true;
                continue;
            }

            if (nextSpace || (chars.Count > 0 && char.IsUpper(ch)))
            {
                chars.Add(separator);
            }

            chars.Add(uppercase ? char.ToUpperInvariant(ch) : char.ToLowerInvariant(ch));
            nextSpace = false;
        }
        
        return chars.ToArray();
    }

    private static char[] ToSentenceCase(string text)
    {
        var sentence = ToSeparatedString(text, ' ', false);
        sentence[0] = char.ToUpper(sentence[0]);
        return sentence;
    }
    
    private static char[] ToCapitalCase(string text, char separator)
    {
        var chars = ToSeparatedString(text, separator, false);
        var nextUpper = true;
        for (int i = 0; i < chars.Length; i++)
        {
            if (nextUpper)
            {
                chars[i] = char.ToUpper(chars[i]);
            }

            nextUpper = chars[i] == separator;
        }
        
        return chars;
    }
    
    private static char[] ToMockingCase(string input)
    {
        var characters = input.ToCharArray();
        var makeUpper = true;
        for (int i = 0; i < characters.Length; i++)
        {
            if (char.IsLetter(characters[i]))
            {
                characters[i] = makeUpper ? char.ToUpper(characters[i]) : char.ToLower(characters[i]);
            }
            
            makeUpper = !makeUpper;
        }

        return characters;
    }

    #region Nested Types

    public enum CaseType
    {
        None,
        LowerCase,          // loremipsum dolor
        UpperCase,          // LOREMIPSUM DOLOR
        CamelCase,          // loremIpsumDolor
        CapitalCase,        // Lorem Ipsum Dolor
        ConstantCase,       // LOREM_IPSUM_DOLOR
        DotCase,            // lorem.ipsum.dolor
        HeaderCase,         // Lorem-Ipsum-Dolor
        NoCase,             // lorem ipsum dolor
        ParamCase,          // lorem-ipsum-dolor
        PascalCase,         // LoremIpsumDolor
        PathForwardCase,    // lorem/ipsum/dolor
        PathBackwardCase,   // lorem\ipsum\dolor
        SentenceCase,       // Lorem ipsum dolor
        SnakeCase,          // lorem_ipsum_dolor
        MockingCase,        // LoReMiPsUm dOlOr
    }

    public record Args : ToolArgs
    {
        public string Text { get; init; } = null!;
        public CaseType Type { get; init; }
    }

    public record Result(string Text) : ToolResult
    {
        public Result() : this(string.Empty){}
    }
    
    #endregion
}