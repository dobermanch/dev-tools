namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "roman-converter",
    Aliases = [],
    Keywords = [Keyword.Generate, Keyword.Text, Keyword.String],
    Categories = [Category.Converter]
)]
public sealed class IntegerToRomanConverterTool : ToolBase<IntegerToRomanConverterTool.Args, IntegerToRomanConverterTool.Result>
{
    private static readonly Dictionary<char, int> RomanToNumber = new()
    {
        ['I'] = 1, ['V'] = 5, 
        ['X'] = 10, ['L'] = 50,
        ['C'] = 100, ['D'] = 500, ['M'] = 1000
    };
    
    private static readonly List<(int, string)> NumberToRoman =
    [
        (1000, "M"),
        (900, "CM"), (500, "D"), (400, "CD"), (100, "C"),
        (90, "XC"), (50, "L"), (40, "XL"), (10, "X"),
        (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
    ];

    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Number))
        {
            throw new ToolException(ErrorCode.TextEmpty);
        }

        var result = args.Transcoding == TranscodingType.Decode 
            ? FromRoman(args.Number) 
            : ToRoman(args.Number);

        return new(result);
    }

    private static string ToRoman(string input)
    {
        if (!int.TryParse(input, out var number) || number < 1 || number > 3999)
        {
            throw new ToolException(ErrorCode.WrongFormat);
        }

        var result = new StringBuilder();
        foreach (var (value, numeral) in NumberToRoman)
        {
            while (number >= value)
            {
                result.Append(numeral);
                number -= value;
            }
        }
        return result.ToString();
    }
    
    private static string FromRoman(string roman)
    {
        int total = 0;
        int prev = 0;

        foreach (char c in roman.ToUpperInvariant())
        {
            if (!RomanToNumber.TryGetValue(c, out int value))
            {
                throw new ToolException(ErrorCode.WrongFormat);
            }

            total += value > prev ? value - 2 * prev : value;
            prev = value;
        }

        return total.ToString();
    }

    public enum TranscodingType
    {
        Encode,
        Decode
    }
    
    public record Args(string Number, TranscodingType Transcoding);

    public record Result(string? Number) : ToolResult
    {
        public Result() : this(Number: null) { }
    }
}
