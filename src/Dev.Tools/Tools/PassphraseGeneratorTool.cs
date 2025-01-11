using Dev.Tools.Providers;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "passphrase-generator",
    Aliases = ["passphrase"],
    Keywords = [Keyword.Generate, Keyword.Token, Keyword.Text, Keyword.String, Keyword.Password],
    Categories = [Category.Text, Category.Crypto, Category.Security],
    ErrorCodes = []
)]
public sealed class PassphraseGeneratorTool : ToolBase<PassphraseGeneratorTool.Args, PassphraseGeneratorTool.Result>
{
    protected override Result Execute(Args args)
    {
        var wordCount = Math.Max(1, args.WordCount);
        var phraseCount = Math.Max(1, args.PhraseCount);
        var rand = new Random();

        var phrases = new string[phraseCount];
        Parallel.For(0, phraseCount, index =>
        {
            var parts = new string[wordCount];
            for (var i = 0; i < wordCount; i++)
            {
                var source = i == wordCount - 1 
                    ? WordProvider.AllCreatures 
                    : i == 0 
                        ? WordProvider.Attributes 
                        : WordProvider.Words;
                parts[i] = source[rand.Next(source.Length)];
            }

            var pass = string.Join(args.Separator?.ToString() ?? "", parts).ToLower();
            if (args.Capitalize && pass.Length >= 1)
            {
                pass = char.ToUpper(pass[0]) + pass.Substring(1);
            }

            if (!string.IsNullOrEmpty(args.Salt))
            {
                pass += args.Salt;
            }
            
            phrases[index] = pass;
        });
        
        return new Result(phrases);
    }

    public record Args : ToolArgs
    {
        public int PhraseCount { get; set; } = 1;
        public int WordCount { get; set; } = 1;
        public string? Salt { get; set; }
        public char? Separator { get; set; } = '-';
        public bool Capitalize { get; set; }
    }

    public record Result(string[] Phrases) : ToolResult
    {
        public Result() : this([]) { }
    }
}