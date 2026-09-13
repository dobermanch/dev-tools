using Dev.Tools.Cryptography;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "token-generator",
    Title = "Token Generator",
    Description =
        "Generates random tokens of a given length from a character set built out of uppercase letters, lowercase letters, digits and symbols, or from a custom alphabet. Characters are drawn with a cryptographic random source.",
    Aliases = ["tokengen"],
    Keywords = [Keyword.Generate, Keyword.Token, Keyword.Text, Keyword.String, Keyword.Password],
    Categories = [Category.Text, Category.Crypto, Category.Security]
)]
public sealed class TokenGeneratorTool(IRandomSource? random = null)
    : ToolBase<TokenGeneratorTool.Args, TokenGeneratorTool.Result>
{
    private readonly IRandomSource _random = random ?? CryptographicRandomSource.Instance;

    protected override Result Execute(Args args, CancellationToken cancellationToken)
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

        // Checked after exclusions: excluding every character leaves nothing to pick from.
        if (alphabet.Count <= 0)
        {
            return new Result(Enumerable.Repeat(new string(' ', length), count).ToArray());
        }

        var finalAlphabet = alphabet.ToArray();

        var result = new string[count];
        Parallel.For(0, count, new ParallelOptions { CancellationToken = cancellationToken }, index =>
        {
            var builder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                // The default source is thread-safe and cryptographically secure;
                // a shared System.Random is neither, and this generates passwords.
                builder.Append(finalAlphabet[_random.Next(finalAlphabet.Length)]);
            }

            result[index] = builder.ToString();
        });

        return new Result(result);
    }

    public sealed record Args(
        [property: Description("Length of each generated token; 0 or less produces empty tokens")]
        int TokenLength = 15,
        [property: DisplayName("Lowercase Letters"),
                   Description("Include lowercase letters (a-z)")]
        bool Lowercase = true,
        [property: Description("Include digits (0-9)")]
        bool Numbers = true,
        [property: DisplayName("Uppercase Letters"),
                   Description("Include uppercase letters (A-Z)")]
        bool Uppercase = true,
        [property: Description("Include special characters (!@#$%^&*...)")]
        bool Symbols = true,
        [property: Description("Number of tokens to generate; values below 1 are treated as 1")]
        int TokenCount = 1,
        [property: Description("Characters removed from the alphabet after it is built")]
        string? ExcludeSymbols = null,
        [property: DisplayName("Custom Alphabet"),
                   Description("Exact character set to draw from; when set, the four character-class options are ignored")]
        string? Alphabet = null
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("Generated Tokens"),
                   Description("The generated tokens, one per requested count")]
        string[] Tokens
    ) : ToolResult
    {
        public Result() : this([])
        {
        }
    }
}