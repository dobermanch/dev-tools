namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "ulid-generator",
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
        Random,
        Min,
        Max
    }
    
    public enum UlidCase
    {
        Lowercase,
        Uppercase
    }

    public enum UlidBrackets
    {
        None,
        Braces,
        Parentheses,
        SquareBrackets
    }

    public sealed record Args(
        UlidType Type = UlidType.Random,
        int Count = 1,
        UlidCase Case = UlidCase.Uppercase,
        UlidBrackets Brackets = UlidBrackets.None
    );

    public sealed record Result([property: PipeOutput] IReadOnlyCollection<string> Data) : ToolResult
    {
        public Result() : this([]) { }
    }
}
