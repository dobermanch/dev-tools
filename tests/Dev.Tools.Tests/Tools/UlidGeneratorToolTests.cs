using Dev.Tools.Tools;
using FluentAssertions;

namespace Dev.Tools.Tests.Tools;

public class UlidGeneratorToolTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task WhenTypeIsMin_ShoudGenerateAllMinUlid(int count)
    {
        var args = new UlidGeneratorTool.Args(UlidGeneratorTool.UlidType.Min, count);

        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(count);
        result.Data.All(it => it == Ulid.MinValue);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task WhenTypeIsMax_ShoudGenerateAllMaxUlid(int count)
    {
        var args = new UlidGeneratorTool.Args(UlidGeneratorTool.UlidType.Max, count);

        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(count);
        result.Data.All(it => it == Ulid.MaxValue);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task WhenTypeIsRandom_ShoudGenerateAllRandomUlid(int count)
    {
        var args = new UlidGeneratorTool.Args(UlidGeneratorTool.UlidType.Random, count);

        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(count);
        result.Data.All(it => it != Ulid.MaxValue && it != Ulid.MinValue);
    }
}
