using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class UlidGeneratorToolTests
{
    [Test]
    [Arguments(1)]
    [Arguments(5)]
    public async Task WhenTypeIsMin_ShouldGenerateAllMinUlid(int count)
    {
        var args = new UlidGeneratorTool.Args
        {
            Type = UlidGeneratorTool.UlidType.Min,
            Count = count
        };

        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).HasCount(count);

        await Assert.That(result.Data).All().Satisfy(it => it, it => it.IsEqualTo(Ulid.MinValue));
    }

    [Test]
    [Arguments(1)]
    [Arguments(5)]
    public async Task WhenTypeIsMax_ShouldGenerateAllMaxUlid(int count)
    {
        var args = new UlidGeneratorTool.Args
        {
            Type = UlidGeneratorTool.UlidType.Max,
            Count = count
        };

        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).HasCount(count);
        
        await Assert.That(result.Data).All().Satisfy(it => it, it => it.IsEqualTo(Ulid.MaxValue));
    }

    [Test]
    [Arguments(1)]
    [Arguments(5)]
    public async Task WhenTypeIsRandom_ShouldGenerateAllRandomUlid(int count)
    {
        var args = new UlidGeneratorTool.Args
        {
            Type = UlidGeneratorTool.UlidType.Random,
            Count = count
        };

        var result = await new UlidGeneratorTool().RunAsync(args, CancellationToken.None);
        
        await Assert.That(result.Data).HasCount(count);

        await Assert.That(result.Data).All().Satisfy(it => it,
            it => it.IsNotEqualTo(Ulid.MinValue).And.IsNotEqualTo<Ulid>(Ulid.MaxValue));
    }
}
