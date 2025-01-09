namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "token-generator",
    Aliases = ["tokengen"],
    Keywords = [Keyword.Generate, Keyword.Token, Keyword.Text, Keyword.String, Keyword.Password],
    Categories = [Category.Text, Category.Crypto, Category.Security],
    ErrorCodes = []
)]
public sealed class TokenGeneratorTool : ToolBase<TokenGeneratorTool.Args, TokenGeneratorTool.Result>
{
    protected override Result Execute(Args args)
    {
        var alphabetBuilder = new StringBuilder(args.Alphabet);
        if (args.Uppercase)
        {
            alphabetBuilder.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }

        if (args.Lowercase)
        {
            alphabetBuilder.Append("abcdefghijklmnopqrstuvwxyz");
        }

        if (args.Numbers)
        {
            alphabetBuilder.Append("0123456789");
        }

        if (args.Symbols)
        {
            alphabetBuilder.Append("!@#$%^&*()_+-=[]{}|;:,.<>?");
        }
        
        var alphabet = alphabetBuilder.ToString().ToHashSet();
        
        var count = args.Count <= 1 ? 1 : args.Count;
        var length = args.Length <= 0 ? 0 : args.Length;
        if (alphabet.Count <= 0)
        {
            return new Result(Enumerable.Repeat(new string(' ', length), count).ToArray());
        }

        if (!string.IsNullOrEmpty(args.ExcludeSymbols))
        {
            foreach (var symbol in args.ExcludeSymbols)
            {
                if (alphabet.Contains(symbol))
                {
                    alphabet.Remove(symbol);
                }
            }
        }

        var finalAlphabet = alphabet.ToArray();
        var random = new Random();

        var result = new string[count];
        Parallel.For(0, count, index =>
        {
            var builder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                builder.Append(finalAlphabet[random.Next(alphabet.Count)]);
            }

            result[index] = builder.ToString();
        });

        return new Result(result);
    }

    public record Args : ToolArgs
    {
        public int Length { get; init; } = 1;
        public bool Lowercase { get; init; }
        public bool Numbers { get; init; }
        public bool Uppercase { get; init; }
        public bool Symbols { get; init; }
        public int Count { get; init; } = 1;
        public string? ExcludeSymbols { get; init; }
        public string? Alphabet { get; init; }
    }

    public record Result(string[] Tokens) : ToolResult
    {
        public Result() : this([])
        {
        }
    }
}