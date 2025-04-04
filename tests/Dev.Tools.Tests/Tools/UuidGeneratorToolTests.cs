﻿using Dev.Tools.Tools;
using FluentAssertions;

namespace Dev.Tools.Tests.Tools;

public class UuidGeneratorToolTests
{
    // -------------------------------
    // NIL tests 

    [Fact]
    public async Task WhenTypeIsNil_ShouldGenerateEmptyUuid()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.Nil
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Should().BeEmpty();
    }

    [Fact]
    public async Task WhenTypeIsNilAndCountProvided_ShouldGenerateRightAmountOfData()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.Nil,
            Count = 5
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(args.Count);
        result.Data.Should().AllBeEquivalentTo(Guid.Empty);
    }

    [Fact]
    public async Task WhenTypeIsNilAndNameProvided_ShouldIgnoreName()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.Nil,
            Name = "name"
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Should().BeEmpty();
    }

    [Fact]
    public async Task WhenTypeIsNilAndNamespaceProvided_ShouldIgnoreNamespace()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.Nil,
            Namespace = Guid.NewGuid()
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Should().BeEmpty();
    }

    // -------------------------------
    // Max tests 

    [Fact]
    public async Task WhenTypeIsMax_ShoudGenerateEmptyUuid()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.Max
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Should().Be(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    [Fact]
    public async Task WhenTypeIsMaxAndCountProvided_ShoudGenerateRightAmountOfData()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.Max,
            Count = 5
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(args.Count);
        result.Data.Should().AllBeEquivalentTo(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    [Fact]
    public async Task WhenTypeIsMaxAndNameProvided_ShouldIgnoreName()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.Max,
            Name = "name"
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1); 
        result.Data.First().Should().Be(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    [Fact]
    public async Task WhenTypeIsMaxAndNamespaceProvided_ShouldIgnoreNamespace()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.Max,
            Namespace = Guid.NewGuid()
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Should().Be(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    // -------------------------------
    // V3 tests 

    [Fact]
    public async Task WhenTypeIsV3AndNamespaceNotProvided_ShouldReturnErrorCode()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V3
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().BeEmpty();
        result.ErrorCodes.First().Should().Be(ErrorCode.NamespaceEmpty);
    }

    [Fact]
    public async Task WhenTypeIsV3AndNameIsNotProvided_ShouldGenerateGuilds()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V3,
            Namespace = Guid.NewGuid()
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Should().NotBe(args.Namespace!.Value);
    }

    [Fact]
    public async Task WhenTypeIsV3AndNameAndNamespaceProvided_ShouldGenerateGuids()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V3,
            Namespace = Guid.NewGuid(),
            Name = "dev-tools"
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Should().NotBe(args.Namespace!.Value);
    }

    [Fact]
    public async Task WhenTypeIsV3AndNameAndNamespaceProvided_ShouldAlwaysGenerateTheSameGuids()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V3,
            Namespace = Guid.NewGuid(),
            Name = "dev-tools"
        };

        var result1 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);
        var result2 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result1.Data.First().Should().Be(result2.Data.First());
    }

    [Fact]
    public async Task WhenTypeIsV3AndNameAndNamespaceAndCountProvided_ShouldGenerateRightAmountOfData()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V3,
            Namespace = Guid.NewGuid(),
            Name = "dev-tools",
            Count = 5
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(args.Count);
    }

    // -------------------------------
    // V4 tests 

    [Fact]
    public async Task WhenTypeIsV4_ShouldGenerateRandomUuid()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V4
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Should().NotBeEmpty();
        result.Data.First().Should().NotBe(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    [Fact]
    public async Task WhenTypeIsV4AndCountProvided_ShouldGenerateRightAmountOfData()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V4,
            Count = 5
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(args.Count);
        result.Data.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task WhenTypeIsV4AndNameProvided_ShouldIgnoreName()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V4,
            Name = "name"
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task WhenTypeIsV4AndNamespaceProvided_ShouldIgnoreNamespace()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V4,
            Namespace = Guid.NewGuid()
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task WhenTypeIsV4_ShouldAlwaysReturnsUniqueData()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V4,
            Namespace = Guid.NewGuid()
        };

        var result1 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);
        var result2 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result1.Data.First().Should().NotBe(result2.Data.First());
    }

    // -------------------------------
    // V5 tests 

    [Fact]
    public async Task WhenTypeIsV5AndNamespaceNotProvided_ShouldReturnErrorCode()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V5
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().BeEmpty();
        result.ErrorCodes.First().Should().Be(ErrorCode.NamespaceEmpty);
    }

    [Fact]
    public async Task WhenTypeIsV5AndNameIsNotProvided_ShouldGenerateGuilds()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V5,
            Namespace = Guid.NewGuid()
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Should().NotBe(args.Namespace!.Value);
    }

    [Fact]
    public async Task WhenTypeIsV5AndNameAndNamespaceProvided_ShouldGenerateGuids()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V5,
            Namespace = Guid.NewGuid(),
            Name = "dev-tools"
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Should().NotBe(args.Namespace!.Value);
    }

    [Fact]
    public async Task WhenTypeIsV5AndNameAndNamespaceProvided_ShouldAlwaysGenerateTheSameGuids()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V5,
            Namespace = Guid.NewGuid(),
            Name = "dev-tools"
        };

        var result1 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);
        var result2 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result1.Data.First().Should().Be(result2.Data.First());
    }

    [Fact]
    public async Task WhenTypeIsV5AndNameAndNamespaceAndCountProvided_ShouldGenerateRightAmountOfData()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V5,
            Namespace = Guid.NewGuid(),
            Name = "dev-tools",
            Count = 5
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(args.Count);
    }

    // -------------------------------
    // V7 tests 

    [Fact]
    public async Task WhenTypeIsV7_ShouldGenerateUuid()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V7
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Should().NotBeEmpty();
        result.Data.First().Should().NotBe(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    [Fact]
    public async Task WhenTypeIsV7AndTimeProvided_ShouldAlwaysGenerateSimilarUuid()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V7,
            Time = DateTime.UtcNow
        };

        var result1 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);
        var result2 = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        byte[] bytes1 = result1.Data.First().ToByteArray()[..5];
        byte[] bytes2 = result2.Data.First().ToByteArray()[..5];

        bytes1.Should().BeEquivalentTo(bytes2);
    }

    [Fact]
    public async Task WhenTypeIsV7AndTimeAndCountProvided_ShouldAlwaysGenerateUniqueUuid()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V7,
            Time = DateTime.UtcNow,
            Count = 5
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(args.Count);
        result.Data.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task WhenTypeIsV7AndCountProvided_ShouldGenerateRightAmountOfData()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V7, 
            Count = 5
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(args.Count);
    }

    [Fact]
    public async Task WhenTypeIsV7AndNameProvided_ShouldIgnoreName()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V7,
            Name = "name"
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Should().NotBeEmpty();
        result.Data.First().Should().NotBe(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }

    [Fact]
    public async Task WhenTypeIsV7AndNamespaceProvided_ShouldIgnoreNamespace()
    {
        var args = new UuidGeneratorTool.Args
        {
            Type = UuidGeneratorTool.UuidType.V7,
            Namespace = Guid.NewGuid()
        };

        var result = await new UuidGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.First().Should().NotBeEmpty();
        result.Data.First().Should().NotBe(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }
}
