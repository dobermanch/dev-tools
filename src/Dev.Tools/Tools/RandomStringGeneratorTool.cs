namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "random-string",
    Aliases = ["rstr"],
    Keywords = [Keyword.Generate, Keyword.Text, Keyword.String, Keyword.Password],
    Categories = [Category.Text, Category.Crypto, Category.Security],
    ErrorCodes = []
)]
public sealed class RandomStringGeneratorTool : ToolBase<RandomStringGeneratorTool.Args, RandomStringGeneratorTool.Result>
{
    protected override Result Execute(Args args)
    {
        var alphabet = args.CustomAlphabet;

        if (alphabet is null)
        {
            if (args.Uppercase)
            {
                alphabet += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            }

            if (args.Lowercase)
            {
                alphabet += "abcdefghijklmnopqrstuvwxyz";
            }

            if (args.Numbers)
            {
                alphabet += "0123456789";
            }

            if (args.Symbols)
            {
                alphabet += "!@#$%^&*()_+-=[]{}|;:,.<>?";
            }
        }

        if (alphabet is null)
        {
            return new(new string(' ', args.Length));
        }

        var random = new Random();

        var result = new StringBuilder(args.Length);
        for (int i = 0; i < args.Length; i++)
        {
            result.Append(alphabet[random.Next(alphabet.Length)]);
        }

        return new Result(result.ToString());
    }

    public record Args(
        int Length,
        bool Lowercase = true,
        bool Numbers = false,
        bool Uppercase = false,
        bool Symbols = false,
        string? CustomAlphabet = null
    ) : ToolArgs;

    public record Result(string Text) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}
