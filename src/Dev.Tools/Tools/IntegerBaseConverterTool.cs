namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "int-base-converter",
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
            ? BitConverter.ToInt64(Convert.FromBase64String(args.InputValue))
            : customInputBase
                ? FromCustomBase(args.InputValue, (int)args.InputBase)
                : Convert.ToInt64(args.InputValue, (int)args.InputBase);

        var result = args.TargetBase == BaseType.Base64
            ? Convert.ToBase64String(BitConverter.GetBytes(input), 0)
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
        Binary = 2,
        Octal = 8,
        Decimal = 10,
        Hexadecimal = 16,
        Base64 = 64
    }

    public record Args([property: PipeInput] string InputValue, BaseType InputBase, BaseType TargetBase);

    public record Result([property: PipeOutput] string? Data, BaseType Base) : ToolResult
    {
        public Result() : this(null!, default) { }
    }
}