using Dev.Tools.Tools;
using FluentAssertions;

namespace Dev.Tools.Tests.Tools;

public class UuidGeneratorToolTests
{
    // -------------------------------
    // NIL tests 

    [Fact]
    public async Task WhenTypeIsNil_ShoudGenerateEmptyUuid()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.Nil);

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
        result.Guilds.First().Should().BeEmpty();
    }

    [Fact]
    public async Task WhenTypeIsNilAndCountProvided_ShoudGenerateRightAmountOfUuids()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.Nil, Count: 5);

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(args.Count);
        result.Guilds.Should().AllBeEquivalentTo(Guid.Empty);
    }

    [Fact]
    public async Task WhenTypeIsNilAndNameProvided_ShouldIgnoreName()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.Nil, Name: "name");

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
        result.Guilds.First().Should().BeEmpty();
    }

    [Fact]
    public async Task WhenTypeIsNilAndNamespaceProvided_ShouldIgnoreNamespace()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.Nil, Namespace: Guid.NewGuid());

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
        result.Guilds.First().Should().BeEmpty();
    }

    // -------------------------------
    // Max tests 

    [Fact]
    public async Task WhenTypeIsMax_ShoudGenerateEmptyUuid()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.Max);

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
        result.Guilds.First().Should().Be(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    [Fact]
    public async Task WhenTypeIsMaxAndCountProvided_ShoudGenerateRightAmountOfUuids()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.Max, Count: 5);

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(args.Count);
        result.Guilds.Should().AllBeEquivalentTo(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    [Fact]
    public async Task WhenTypeIsMaxAndNameProvided_ShouldIgnoreName()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.Max, Name: "name");

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1); 
        result.Guilds.First().Should().Be(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    [Fact]
    public async Task WhenTypeIsMaxAndNamespaceProvided_ShouldIgnoreNamespace()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.Max, Namespace: Guid.NewGuid());

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
        result.Guilds.First().Should().Be(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    // -------------------------------
    // V3 tests 

    [Fact]
    public async Task WhenTypeIsV3AndNamespaceNotProvided_ShoudReturnErrorCode()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.V3);

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().BeEmpty();
        result.ErrorCodes.First().Code.Should().Be("NAMESPACE_EMPTY");
    }

    [Fact]
    public async Task WhenTypeIsV3AndNameIsNotProvided_ShoudGenerateGuilds()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.V3, Namespace: Guid.NewGuid());

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
        result.Guilds.First().Should().NotBe(args.Namespace!.Value);
    }

    [Fact]
    public async Task WhenTypeIsV3AndNameAndNamespaceProvided_ShoudGenerateGuids()
    {
        var args = new UuidGeneratorTool.Args(
            UuidGeneratorTool.UuidType.V3, 
            Namespace: Guid.NewGuid(), 
            Name: "dev-tools"
        );

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
        result.Guilds.First().Should().NotBe(args.Namespace!.Value);
    }

    [Fact]
    public async Task WhenTypeIsV3AndNameAndNamespaceProvided_ShoudAlwaysGenerateTheSameGuids()
    {
        var args = new UuidGeneratorTool.Args(
            UuidGeneratorTool.UuidType.V3,
            Namespace: Guid.NewGuid(),
            Name: "dev-tools"
        );

        var result1 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);
        var result2 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result1.Guilds.First().Should().Be(result2.Guilds.First());
    }

    [Fact]
    public async Task WhenTypeIsV3AndNameAndNamespaceAndCountProvided_ShoudGenerateRightAmountOfUuids()
    {
        var args = new UuidGeneratorTool.Args(
            UuidGeneratorTool.UuidType.V3,
            Namespace: Guid.NewGuid(),
            Name: "dev-tools",
            Count: 5
        );

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(args.Count);
    }

    // -------------------------------
    // V4 tests 

    [Fact]
    public async Task WhenTypeIsV4_ShoudGenerateRandomUuid()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.V4);

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
        result.Guilds.First().Should().NotBeEmpty();
        result.Guilds.First().Should().NotBe(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    [Fact]
    public async Task WhenTypeIsV4AndCountProvided_ShoudGenerateRightAmountOfUuids()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.V4, Count: 5);

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(args.Count);
        result.Guilds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task WhenTypeIsV4AndNameProvided_ShouldIgnoreName()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.V4, Name: "name");

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
    }

    [Fact]
    public async Task WhenTypeIsV4AndNamespaceProvided_ShouldIgnoreNamespace()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.V4, Namespace: Guid.NewGuid());

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
    }

    [Fact]
    public async Task WhenTypeIsV4_ShouldAlwaysReturnsUniqueUuids()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.V4, Namespace: Guid.NewGuid());

        var result1 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);
        var result2 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result1.Guilds.First().Should().NotBe(result2.Guilds.First());
    }

    // -------------------------------
    // V5 tests 

    [Fact]
    public async Task WhenTypeIsV5AndNamespaceNotProvided_ShoudReturnErrorCode()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.V5);

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().BeEmpty();
        result.ErrorCodes.First().Code.Should().Be("NAMESPACE_EMPTY");
    }

    [Fact]
    public async Task WhenTypeIsV5AndNameIsNotProvided_ShoudGenerateGuilds()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.V5, Namespace: Guid.NewGuid());

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
        result.Guilds.First().Should().NotBe(args.Namespace!.Value);
    }

    [Fact]
    public async Task WhenTypeIsV5AndNameAndNamespaceProvided_ShoudGenerateGuids()
    {
        var args = new UuidGeneratorTool.Args(
            UuidGeneratorTool.UuidType.V5,
            Namespace: Guid.NewGuid(),
            Name: "dev-tools"
        );

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
        result.Guilds.First().Should().NotBe(args.Namespace!.Value);
    }

    [Fact]
    public async Task WhenTypeIsV5AndNameAndNamespaceProvided_ShoudAlwaysGenerateTheSameGuids()
    {
        var args = new UuidGeneratorTool.Args(
            UuidGeneratorTool.UuidType.V5,
            Namespace: Guid.NewGuid(),
            Name: "dev-tools"
        );

        var result1 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);
        var result2 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result1.Guilds.First().Should().Be(result2.Guilds.First());
    }

    [Fact]
    public async Task WhenTypeIsV5AndNameAndNamespaceAndCountProvided_ShoudGenerateRightAmountOfUuids()
    {
        var args = new UuidGeneratorTool.Args(
            UuidGeneratorTool.UuidType.V5,
            Namespace: Guid.NewGuid(),
            Name: "dev-tools",
            Count: 5
        );

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(args.Count);
    }

    // -------------------------------
    // V7 tests 

    [Fact]
    public async Task WhenTypeIsV7_ShoudGenerateUuid()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.V7);

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
        result.Guilds.First().Should().NotBeEmpty();
        result.Guilds.First().Should().NotBe(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    [Fact]
    public async Task WhenTypeIsV7AndTimeProvided_ShoudAlwaysGenerateSimilarUuid()
    {
        var args = new UuidGeneratorTool.Args(
            UuidGeneratorTool.UuidType.V7,
            time: DateTimeOffset.UtcNow
        );

        var result1 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);
        var result2 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        byte[] bytes1 = result1.Guilds.First().ToByteArray()[..5];
        byte[] bytes2 = result2.Guilds.First().ToByteArray()[..5];

        bytes1.Should().BeEquivalentTo(bytes2);
    }

    [Fact]
    public async Task WhenTypeIsV7AndTimeAndCountProvided_ShoudAlwaysGenerateUniqueUuid()
    {
        var args = new UuidGeneratorTool.Args(
            UuidGeneratorTool.UuidType.V7,
            time: DateTimeOffset.UtcNow,
            Count: 5
        );

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(args.Count);
        result.Guilds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task WhenTypeIsV7AndCountProvided_ShoudGenerateRightAmountOfUuids()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.V7, Count: 5);

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(args.Count);
    }

    [Fact]
    public async Task WhenTypeIsV7AndNameProvided_ShouldIgnoreName()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.V7, Name: "name");

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
        result.Guilds.First().Should().NotBeEmpty();
        result.Guilds.First().Should().NotBe(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    [Fact]
    public async Task WhenTypeIsV7AndNamespaceProvided_ShouldIgnoreNamespace()
    {
        var args = new UuidGeneratorTool.Args(UuidGeneratorTool.UuidType.V7, Namespace: Guid.NewGuid());

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Guilds.Should().HaveCount(1);
        result.Guilds.First().Should().NotBeEmpty();
        result.Guilds.First().Should().NotBe(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }
}
