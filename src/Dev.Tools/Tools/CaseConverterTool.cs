namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "case-converter",
    Title = "Case Converter",
    Description =
        "Converts text between casing conventions: camelCase, PascalCase, snake_case, kebab-case, CONSTANT_CASE, dot.case, path case and several others.",
    Aliases = ["case"],
    Keywords = [Keyword.Text, Keyword.String, Keyword.Convert, Keyword.Case],
    Categories = [Category.Converter, Category.Text]
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
        [Description("None - the text is returned unchanged")]
        None,

        [Description("lowercase - loremipsum dolor")]
        LowerCase,

        [Description("UPPERCASE - LOREMIPSUM DOLOR")]
        UpperCase,

        [Description("camelCase - loremIpsumDolor")]
        CamelCase,

        [Description("Capital Case - Lorem Ipsum Dolor")]
        CapitalCase,

        [Description("CONSTANT_CASE - LOREM_IPSUM_DOLOR")]
        ConstantCase,

        [Description("dot.case - lorem.ipsum.dolor")]
        DotCase,

        [Description("Header-Case - Lorem-Ipsum-Dolor")]
        HeaderCase,

        [Description("no case - lorem ipsum dolor")]
        NoCase,

        [Description("param-case, also known as kebab-case - lorem-ipsum-dolor")]
        ParamCase,

        [Description("PascalCase - LoremIpsumDolor")]
        PascalCase,

        [Description("path/forward/case - lorem/ipsum/dolor")]
        PathForwardCase,

        [Description("path\\backward\\case - lorem\\ipsum\\dolor")]
        PathBackwardCase,

        [Description("Sentence case - Lorem ipsum dolor")]
        SentenceCase,

        [Description("snake_case - lorem_ipsum_dolor")]
        SnakeCase,

        [Description("mOcKiNg cAsE - LoReMiPsUm dOlOr")]
        MockingCase,
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Text to convert to a different case format")]
        string Text,
        [property: DisplayName("Case Type"),
                   Description("Target case format for the conversion")]
        CaseType Type = CaseType.None
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("Converted Text"),
                   Description("The text in the requested case format")]
        string Text
    ) : ToolResult
    {
        public Result() : this(string.Empty){}
    }
    
    #endregion
}