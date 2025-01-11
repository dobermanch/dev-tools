using Dev.Tools.Tools;
using FluentAssertions;

namespace Dev.Tools.Tests.Tools;

public class UlidGeneratorToolTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task WhenTypeIsMin_ShouldGenerateAllMinUlid(int count)
    {
        var args = new UlidGeneratorTool.Args
        {
            Type = UlidGeneratorTool.UlidType.Min,
            Count = count
        };

        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(count);
        result.Data.All(it => it == Ulid.MinValue).Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task WhenTypeIsMax_ShouldGenerateAllMaxUlid(int count)
    {
        var args = new UlidGeneratorTool.Args
        {
            Type = UlidGeneratorTool.UlidType.Max,
            Count = count
        };

        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(count);
        result.Data.All(it => it == Ulid.MaxValue).Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task WhenTypeIsRandom_ShouldGenerateAllRandomUlid(int count)
    {
        var args = new UlidGeneratorTool.Args
        {
            Type = UlidGeneratorTool.UlidType.Random,
            Count = count
        };

        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(count);
        result.Data.All(it => it != Ulid.MaxValue && it != Ulid.MinValue).Should().BeTrue();
    }
}
