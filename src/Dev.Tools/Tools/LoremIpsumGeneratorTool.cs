namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "lorem-ipsum",
    Aliases = ["lorem"],
    Keywords = [Keyword.Generate, Keyword.Text, Keyword.String, Keyword.Lorem],
    Categories = [Category.Text]
)]
public sealed class LoremIpsumGeneratorTool : ToolBase<LoremIpsumGeneratorTool.Args, LoremIpsumGeneratorTool.Result>
{
    protected override Result Execute(Args args)
    {
        var paragraphs = Math.Max(1, args.Paragraphs);
        var sentences = Math.Max(1, args.SentencesPerParagraph);
        var words = Math.Max(1, args.WordsPerSentence);
        var random = new Random();

        var paragraphTexts = new string[paragraphs];
        for (var p = 0; p < paragraphs; p++)
        {
            var sentenceTexts = new string[sentences];
            for (var s = 0; s < sentences; s++)
            {
                sentenceTexts[s] = p == 0 && s == 0 && args.StartWithLoremIpsum
                    ? BuildLoremStart(words, random)
                    : BuildSentence(words, random);
            }

            paragraphTexts[p] = string.Join(" ", sentenceTexts);
        }

        return new Result(string.Join("\n\n", paragraphTexts));
    }

    private static string BuildLoremStart(int wordCount, Random random)
    {
        string[] opening = ["Lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit"];

        if (wordCount <= opening.Length)
        {
            return string.Join(" ", opening.Take(wordCount)) + ".";
        }

        var extra = Enumerable
            .Range(0, wordCount - opening.Length)
            .Select(_ => Words[random.Next(Words.Length)]);
        return string.Join(" ", opening.Concat(extra)) + ".";
    }

    private static string BuildSentence(int wordCount, Random random)
    {
        var picked = Enumerable.Range(0, wordCount)
            .Select(_ => Words[random.Next(Words.Length)])
            .ToArray();
        
        picked[0] = char.ToUpper(picked[0][0]) + picked[0][1..];
        
        return string.Join(" ", picked) + ".";
    }

    // ~100-word Lorem Ipsum vocabulary (from Cicero / de Finibus Bonorum et Malorum)
    private static readonly string[] Words =
    [
        "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing",
        "elit", "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore",
        "et", "dolore", "magna", "aliqua", "enim", "ad", "minim", "veniam",
        "quis", "nostrud", "exercitation", "ullamco", "laboris", "nisi", "aliquip",
        "ex", "ea", "commodo", "consequat", "duis", "aute", "irure", "in",
        "reprehenderit", "voluptate", "velit", "esse", "cillum", "fugiat", "nulla",
        "pariatur", "excepteur", "sint", "occaecat", "cupidatat", "non", "proident",
        "sunt", "culpa", "qui", "officia", "deserunt", "mollit", "anim", "id",
        "est", "laborum", "perspiciatis", "unde", "omnis", "iste", "natus",
        "accusantium", "doloremque", "laudantium", "totam", "rem", "aperiam",
        "eaque", "ipsa", "quae", "ab", "illo", "inventore", "veritatis", "quasi",
        "architecto", "beatae", "vitae", "dicta", "explicabo", "aspernatur", "odit",
        "fugit", "quo", "maxime", "placeat", "facere", "possimus", "voluptas",
        "assumenda", "repellendus", "temporibus", "quibusdam", "officiis", "debitis",
        "necessitatibus", "saepe", "eveniet", "repudiandae", "itaque", "earum"
    ];

    public readonly record struct Args(
        int Paragraphs = 3,
        int SentencesPerParagraph = 5,
        int WordsPerSentence = 8,
        bool StartWithLoremIpsum = true
    );

    public sealed record Result([property: PipeOutput] string Text) : ToolResult
    {
        public Result() : this(string.Empty)
        {
        }
    }
}