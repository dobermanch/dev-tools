using Dev.Tools.Providers;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "passphrase-generator",
    Aliases = ["passphrase"],
    Keywords = [Keyword.Generate, Keyword.Token, Keyword.Text, Keyword.String, Keyword.Password],
    Categories = [Category.Text, Category.Crypto, Category.Security]
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

    public readonly record struct Args(
        int PhraseCount = 1,
        int WordCount  = 5,
        char? Separator  = '-',
        bool Capitalize = false,
        string? Salt = null
    );

    public sealed record Result([property: PipeOutput] string[] Phrases) : ToolResult
    {
        public Result() : this([]) { }
    }
}