namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "nato",
    Title = "NATO Phonetic Alphabet",
    Description =
        "Converts text to NATO phonetic alphabet words (Alfa, Bravo, Charlie, ...) for clear voice communication. Whitespace is dropped; characters outside A-Z are passed through unchanged.",
    Aliases = [],
    Keywords = [Keyword.Generate, Keyword.Text, Keyword.String],
    Categories = [Category.Converter]
)]
public sealed class StringToNatoAlphabetTool : ToolBase<StringToNatoAlphabetTool.Args, StringToNatoAlphabetTool.Result>
{
    private static readonly Dictionary<char, string> NatoAlphabet = new()
    {
        ['A'] = "Alfa",
        ['B'] = "Bravo",
        ['C'] = "Charlie",
        ['D'] = "Delta",
        ['E'] = "Echo",
        ['F'] = "Foxtrot",
        ['G'] = "Golf",
        ['H'] = "Hotel",
        ['I'] = "India",
        ['J'] = "Juliett",
        ['K'] = "Kilo",
        ['L'] = "Lima",
        ['M'] = "Mike",
        ['N'] = "November",
        ['O'] = "Oscar",
        ['P'] = "Papa",
        ['Q'] = "Quebec",
        ['R'] = "Romeo",
        ['S'] = "Sierra",
        ['T'] = "Tango",
        ['U'] = "Uniform",
        ['V'] = "Victor",
        ['W'] = "Whiskey",
        ['X'] = "Xray",
        ['Y'] = "Yankee",
        ['Z'] = "Zulu"
    };
    
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            throw new ToolException(ErrorCode.TextEmpty);
        }

        List<string> words = args.Text
            .Where(ch => !char.IsWhiteSpace(ch))
            .Select(ch => NatoAlphabet.TryGetValue(char.ToUpper(ch), out var word) ? word : ch.ToString())
            .ToList();

        return new(words);
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Text to convert to the NATO phonetic alphabet")]
        string Text
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("NATO Words"),
                   Description("One phonetic word per input character, in order")]
        IReadOnlyCollection<string> Words
    ) : ToolResult
    {
        public Result() : this([]) { }
    }
}
