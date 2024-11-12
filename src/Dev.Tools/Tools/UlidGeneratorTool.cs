namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "ulid",
    Aliases = [],
    Keywords = [Keywords.Uuid, Keywords.Guid, Keywords.Generate, Keywords.Text, Keywords.String],
    Categories = [Categories.Crypto],
    ErrorCodes = []
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

    public record Args(
        UlidType Type,
        int Count = 1
    ) : ToolArgs;

    public record Result(IReadOnlyCollection<Ulid> Data) : ToolResult
    {
        public Result() : this([]) { }
    }
}
