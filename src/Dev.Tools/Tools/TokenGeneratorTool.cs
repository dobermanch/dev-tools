namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "token-generator",
    Aliases = ["tokengen"],
    Keywords = [Keyword.Generate, Keyword.Token, Keyword.Text, Keyword.String, Keyword.Password],
    Categories = [Category.Text, Category.Crypto, Category.Security]
)]
public sealed class TokenGeneratorTool : ToolBase<TokenGeneratorTool.Args, TokenGeneratorTool.Result>
{
    protected override Result Execute(Args args)
    {
        var alphabetBuilder = new StringBuilder(args.Alphabet);
        if (alphabetBuilder.Length <= 0)
        {
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
        }

        var alphabet = alphabetBuilder.ToString().ToHashSet();
        
        var count = args.TokenCount <= 1 ? 1 : args.TokenCount;
        var length = args.TokenLength <= 0 ? 0 : args.TokenLength;
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

    public record Args
    {
        public int TokenLength { get; set; } = 15;
        public bool Lowercase { get; set; } = true;
        public bool Numbers { get; set; } = true;
        public bool Uppercase { get; set; } = true;
        public bool Symbols { get; set; } = true;
        public int TokenCount { get; set; } = 1;
        public string? ExcludeSymbols { get; set; }
        public string? Alphabet { get; set; }
    }

    public record Result([property: PipeOutput] string[] Tokens) : ToolResult
    {
        public Result() : this([])
        {
        }
    }
}