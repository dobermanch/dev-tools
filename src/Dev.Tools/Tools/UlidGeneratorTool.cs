namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "ulid-generator",
    Title = "ULID Generator",
    Description =
        "Generates Universally Unique Lexicographically Sortable Identifiers (ULIDs): 26-character Crockford Base32 values that sort by creation time and are URL-safe.",
    Aliases = ["ulid"],
    Keywords = [Keyword.Uuid, Keyword.Guid, Keyword.Generate, Keyword.Text, Keyword.String],
    Categories = [Category.Crypto]
)]
public sealed class UlidGeneratorTool : ToolBase<UlidGeneratorTool.Args, UlidGeneratorTool.Result>
{
    protected override Result Execute(Args args)
    {
        Ulid[] guids = args.Type switch
        {
            UlidType.Min => Generate(args, () => Ulid.MinValue),
            UlidType.Max => Generate(args, () => Ulid.MaxValue),
            _ => Generate(args, () => Ulid.NewUlid()),
        };

        string[] formattedUlids = guids.Select(u => FormatUlid(u, args)).ToArray();

        return new(formattedUlids);
    }

    private static Ulid[] Generate(Args args, Func<Ulid> getGuid)
    {
        var ulids = new Ulid[args.Count <= 0 ? 1 : args.Count];
        Parallel.For(0, ulids.Length, index => ulids[index] = getGuid());

        return ulids;
    }

    private static string FormatUlid(Ulid ulid, Args args)
    {
        var ulidString = ulid.ToString();

        if (args.Case == UlidCase.Lowercase)
        {
            ulidString = ulidString.ToLowerInvariant();
        }

        return args.Brackets switch
        {
            UlidBrackets.Braces => $"{{{ulidString}}}",
            UlidBrackets.Parentheses => $"({ulidString})",
            UlidBrackets.SquareBrackets => $"[{ulidString}]",
            _ => ulidString
        };
    }

    public enum UlidType : byte
    {
        [Description("A new random ULID stamped with the current time")]
        Random,

        [Description("The smallest possible ULID (all zeros)")]
        Min,

        [Description("The largest possible ULID (all ones)")]
        Max
    }

    public enum UlidCase
    {
        [Description("Lowercase")]
        Lowercase,

        [Description("Uppercase, the canonical ULID form")]
        Uppercase
    }

    public enum UlidBrackets
    {
        [Description("No wrapping characters")]
        None,

        [Description("Wrapped in braces: {...}")]
        Braces,

        [Description("Wrapped in parentheses: (...)")]
        Parentheses,

        [Description("Wrapped in square brackets: [...]")]
        SquareBrackets
    }

    public sealed record Args(
        [property: DisplayName("ULID Type"),
                   Description("Which kind of ULID to generate")]
        UlidType Type = UlidType.Random,
        [property: Description("Number of ULIDs to generate; values below 1 are treated as 1")]
        int Count = 1,
        [property: Description("Letter case of the generated ULIDs")]
        UlidCase Case = UlidCase.Uppercase,
        [property: Description("Characters wrapped around each ULID")]
        UlidBrackets Brackets = UlidBrackets.None
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("ULIDs"),
                   Description("The generated ULIDs, one per requested count")]
        IReadOnlyCollection<string> Data
    ) : ToolResult
    {
        public Result() : this([]) { }
    }
}
