namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "int-base-converter",
    Title = "Integer Base Converter",
    Description =
        "Converts a whole number between binary, octal, decimal, hexadecimal and Base64 representations.",
    Usage =
        "Only the five listed bases are available - there is no arbitrary-radix option. The Base64 form here is a positional base-64 numeral, not the Base64 encoding of bytes; use the base64-encoder tool for that.",
    Aliases = [],
    Keywords = [Keyword.Convert, Keyword.Encode],
    Categories = [Category.Converter]
)]
public sealed class IntegerBaseConverterTool : ToolBase<IntegerBaseConverterTool.Args, IntegerBaseConverterTool.Result>
{
    private static readonly string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    private const int MinBaseLength = 1;
    private const int MaxBaseLength = 64;
    
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.InputValue))
        {
            throw new ToolException(ErrorCode.InputNotValid);
        }

        if ((int)args.InputBase < MinBaseLength
            || (int)args.InputBase > MaxBaseLength
            || (int)args.TargetBase < MinBaseLength
            || (int)args.TargetBase > MaxBaseLength)
        {
            throw new ToolException(ErrorCode.WrongBase);
        }

        if (args.InputBase == args.TargetBase)
        {
            return new Result(args.InputValue, args.TargetBase);
        }

        var standardBases = Enum.GetValues<BaseType>();
        var customInputBase = standardBases.All(it => it != args.InputBase);
        var customTargetBase = standardBases.All(it => it != args.TargetBase);

        var input = args.InputBase == BaseType.Base64
            ? int.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(args.InputValue)))
            : customInputBase
                ? FromCustomBase(args.InputValue, (int)args.InputBase)
                : Convert.ToInt64(args.InputValue, (int)args.InputBase);

        var result = args.TargetBase == BaseType.Base64
            ? Convert.ToBase64String(Encoding.UTF8.GetBytes(input.ToString()))
            : customTargetBase
                ? ToCustomBase((int)input, (int)args.TargetBase)
                : Convert.ToString(input, (int)args.TargetBase);
        
        return new Result(result, args.TargetBase);
    }
    
    private static string ToCustomBase(int value, int baseValue)
    {
        if (value == 0)
        {
            return Alphabet[0].ToString();
        }

        var result = new StringBuilder();
        int current = Math.Abs(value);

        while (current > 0)
        {
            result.Insert(0, Alphabet[current % baseValue]);
            current /= baseValue;
        }

        return value < 0 ? "-" + result : result.ToString();
    }
    
    private static int FromCustomBase(string input, int baseValue)
    {
        int result = 0;
        foreach (char c in input)
        {
            int index = Alphabet.IndexOf(c);
            if (index < 0 || index >= baseValue)
            {
                throw new ToolException(ErrorCode.InputNotValid);
            }

            result = checked(result * baseValue + index);
        }

        return result;
    }
    
    public enum BaseType : int
    {
        [Description("Binary (base 2)")]
        Binary = 2,

        [Description("Octal (base 8)")]
        Octal = 8,

        [Description("Decimal (base 10)")]
        Decimal = 10,

        [Description("Hexadecimal (base 16)")]
        Hexadecimal = 16,

        [Description("Base64 (base 64), digits A-Z a-z 0-9 - _")]
        Base64 = 64
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("The number to convert, written in the input base")]
        string InputValue,
        [property: Description("Base the input value is written in")]
        BaseType InputBase,
        [property: Description("Base to write the result in")]
        BaseType TargetBase
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("Converted Value"),
                   Description("The number written in the target base")]
        string? Data,
        [property: Description("The base the converted value is written in")]
        BaseType Base
    ) : ToolResult
    {
        public Result() : this(null!, default) { }
    }
}