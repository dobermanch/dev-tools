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

        return new(guids);
    }

    private static Ulid[] Generate(Args args, Func<Ulid> getGuid)
    {
        var ulids = new Ulid[args.Count <= 0 ? 1 : args.Count];
        Parallel.For(0, ulids.Length, index => ulids[index] = getGuid());

        return ulids;
    }

    public enum UlidType : byte
    {
        Random,
        Min,
        Max
    }

    public sealed record Args(
        UlidType Type = UlidType.Random,
        int Count = 1
    );

    public sealed record Result([property: PipeOutput] IReadOnlyCollection<Ulid> Data) : ToolResult
    {
        public Result() : this([]) { }
    }
}
