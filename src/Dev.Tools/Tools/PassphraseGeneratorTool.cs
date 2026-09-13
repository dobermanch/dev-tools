using System.Security.Cryptography;
using Dev.Tools.Providers;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "passphrase-generator",
    Title = "Passphrase Generator",
    Description =
        "Generates memorable and secure passphrases by combining random words with optional separators, capitalization, and salt. Words are drawn with a cryptographic random source.",
    Aliases = ["passphrase"],
    Keywords = [Keyword.Generate, Keyword.Token, Keyword.Text, Keyword.String, Keyword.Password],
    Categories = [Category.Text, Category.Crypto, Category.Security]
)]
public sealed class PassphraseGeneratorTool : ToolBase<PassphraseGeneratorTool.Args, PassphraseGeneratorTool.Result>
{
    protected override Result Execute(Args args, CancellationToken cancellationToken)
    {
        var wordCount = Math.Max(1, args.WordCount);
        var phraseCount = Math.Max(1, args.PhraseCount);

        var phrases = new string[phraseCount];
        Parallel.For(0, phraseCount, new ParallelOptions { CancellationToken = cancellationToken }, index =>
        {
            var parts = new string[wordCount];
            for (var i = 0; i < wordCount; i++)
            {
                var source = i == wordCount - 1 
                    ? WordProvider.AllCreatures 
                    : i == 0 
                        ? WordProvider.Attributes 
                        : WordProvider.Words;
                // RandomNumberGenerator is thread-safe and cryptographically secure;
                // a shared System.Random is neither, and this generates passphrases.
                parts[i] = source[RandomNumberGenerator.GetInt32(source.Length)];
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

    public sealed record Args(
        [property: Description("Number of passphrases to generate; values below 1 are treated as 1")]
        int PhraseCount = 1,
        [property: Description("Number of words per passphrase; values below 1 are treated as 1")]
        int WordCount = 5,
        [property: Description("Character placed between words; omit for no separator")]
        char? Separator = '-',
        [property: Description("Capitalize the first letter of each passphrase")]
        bool Capitalize = false,
        [property: Description("Text appended to the end of each passphrase")]
        string? Salt = null
    );

    public sealed record Result(
        [property: PipeOutput,
                   Description("The generated passphrases, one per requested phrase")]
        string[] Phrases
    ) : ToolResult
    {
        public Result() : this([]) { }
    }
}