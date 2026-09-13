using Dev.Tools.Tests.Helpers;
using Dev.Tools.Cryptography;
using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class UuidGeneratorToolTests
{
    // -------------------------------
    // NIL tests 

    [Test]
    public async Task WhenTypeIsNil_ShouldGenerateEmptyUuid()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.Nil
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsEqualTo("00000000-0000-0000-0000-000000000000");
    }

    [Test]
    public async Task WhenTypeIsNilAndCountProvided_ShouldGenerateRightAmountOfData()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.Nil,
            Count: 5
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(args.Count);
        await Assert.That(result.Data).All().Satisfy(it => it.IsEqualTo("00000000-0000-0000-0000-000000000000"));
    }

    [Test]
    public async Task WhenTypeIsNilAndNameProvided_ShouldIgnoreName()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.Nil,
            Name: "name"
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsEqualTo("00000000-0000-0000-0000-000000000000");
    }

    [Test]
    public async Task WhenTypeIsNilAndNamespaceProvided_ShouldIgnoreNamespace()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.Nil,
            Namespace: Guid.NewGuid()
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsEqualTo("00000000-0000-0000-0000-000000000000");
    }

    // -------------------------------
    // Max tests 

    [Test]
    public async Task WhenTypeIsMax_ShoudGenerateEmptyUuid()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.Max
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsEqualTo("ffffffff-ffff-ffff-ffff-ffffffffffff");
    }

    [Test]
    public async Task WhenTypeIsMaxAndCountProvided_ShoudGenerateRightAmountOfData()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.Max,
            Count: 5
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(args.Count);
        await Assert.That(result.Data).All().Satisfy(it => it.IsEqualTo("ffffffff-ffff-ffff-ffff-ffffffffffff"));
    }

    [Test]
    public async Task WhenTypeIsMaxAndNameProvided_ShouldIgnoreName()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.Max,
            Name: "name"
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsEqualTo("ffffffff-ffff-ffff-ffff-ffffffffffff");
    }

    [Test]
    public async Task WhenTypeIsMaxAndNamespaceProvided_ShouldIgnoreNamespace()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.Max,
            Namespace: Guid.NewGuid()
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsEqualTo("ffffffff-ffff-ffff-ffff-ffffffffffff");
    }

    // -------------------------------
    // V3 tests 

    [Test]
    public async Task WhenTypeIsV3AndNamespaceNotProvided_ShouldReturnErrorCode()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V3
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).IsEmpty();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.NamespaceEmpty);
    }

    [Test]
    public async Task WhenTypeIsV3AndNameIsNotProvided_ShouldGenerateGuilds()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V3,
            Namespace: Guid.NewGuid()
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsNotEqualTo(args.Namespace!.Value.ToString("d"));
    }

    [Test]
    public async Task WhenTypeIsV3AndNameAndNamespaceProvided_ShouldGenerateGuids()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V3,
            Namespace: Guid.NewGuid(),
            Name: "dev-tools"
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);
        
        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsNotEqualTo(args.Namespace!.Value.ToString("d"));
    }

    [Test]
    public async Task WhenTypeIsV3AndNameAndNamespaceProvided_ShouldAlwaysGenerateTheSameGuids()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V3,
            Namespace: Guid.NewGuid(),
            Name: "dev-tools"
        );

        var result1 = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);
        var result2 = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);
        
        await Assert.That(result1.Data.First()).IsEqualTo(result2.Data.First());
    }

    [Test]
    public async Task WhenTypeIsV3AndNameAndNamespaceAndCountProvided_ShouldGenerateRightAmountOfData()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V3,
            Namespace: Guid.NewGuid(),
            Name: "dev-tools",
            Count: 5
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(args.Count);
    }

    // -------------------------------
    // V4 tests 

    [Test]
    public async Task WhenTypeIsV4_ShouldGenerateRandomUuid()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V4
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsNotEqualTo("00000000-0000-0000-0000-000000000000");
        await Assert.That(result.Data.First()).IsNotEqualTo("ffffffff-ffff-ffff-ffff-ffffffffffff");
    }

    [Test]
    public async Task WhenTypeIsV4AndCountProvided_ShouldGenerateRightAmountOfData()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V4,
            Count: 5
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(args.Count);
        await Assert.That(result.Data).HasDistinctItems();
    }

    [Test]
    public async Task WhenTypeIsV4AndNameProvided_ShouldIgnoreName()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V4,
            Name: "name"
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
    }

    [Test]
    public async Task WhenTypeIsV4AndNamespaceProvided_ShouldIgnoreNamespace()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V4,
            Namespace: Guid.NewGuid()
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
    }

    [Test]
    public async Task WhenTypeIsV4_ShouldAlwaysReturnsUniqueData()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V4,
            Namespace: Guid.NewGuid()
        );

        var result1 = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);
        var result2 = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);
        
        await Assert.That(result1.Data.First()).IsNotEqualTo(result2.Data.First());
    }

    // -------------------------------
    // V5 tests 

    [Test]
    public async Task WhenTypeIsV5AndNamespaceNotProvided_ShouldReturnErrorCode()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V5
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).IsEmpty();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.NamespaceEmpty);
    }

    [Test]
    public async Task WhenTypeIsV5AndNameIsNotProvided_ShouldGenerateGuilds()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V5,
            Namespace: Guid.NewGuid()
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsNotEqualTo(args.Namespace!.Value.ToString("d"));
    }

    [Test]
    public async Task WhenTypeIsV5AndNameAndNamespaceProvided_ShouldGenerateGuids()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V5,
            Namespace: Guid.NewGuid(),
            Name: "dev-tools"
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsNotEqualTo(args.Namespace!.Value.ToString("d"));
    }

    [Test]
    public async Task WhenTypeIsV5AndNameAndNamespaceProvided_ShouldAlwaysGenerateTheSameGuids()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V5,
            Namespace: Guid.NewGuid(),
            Name: "dev-tools"
        );

        var result1 = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);
        var result2 = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result1.Data.First()).IsEqualTo(result2.Data.First());
    }

    [Test]
    public async Task WhenTypeIsV5AndNameAndNamespaceAndCountProvided_ShouldGenerateRightAmountOfData()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V5,
            Namespace: Guid.NewGuid(),
            Name: "dev-tools",
            Count: 5
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(args.Count);
    }

    // -------------------------------
    // V7 tests 

    [Test]
    public async Task WhenTypeIsV7_ShouldGenerateUuid()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V7
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsNotEqualTo("00000000-0000-0000-0000-000000000000");
        await Assert.That(result.Data.First()).IsNotEqualTo("ffffffff-ffff-ffff-ffff-ffffffffffff");
    }

    [Test]
    public async Task WhenTypeIsV7AndTimeProvided_ShouldAlwaysGenerateSimilarUuid()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V7,
            Time: DateTime.UtcNow
        );

        var result1 = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);
        var result2 = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        byte[] bytes1 = Guid.Parse(result1.Data.First()).ToByteArray()[..5];
        byte[] bytes2 = Guid.Parse(result2.Data.First()).ToByteArray()[..5];

        await Assert.That(bytes1).IsEquivalentTo(bytes2);
    }

    [Test]
    public async Task WhenTypeIsV7AndTimeAndCountProvided_ShouldAlwaysGenerateUniqueUuid()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V7,
            Time: DateTime.UtcNow,
            Count: 5
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(args.Count);
        await Assert.That(result.Data).HasDistinctItems();
    }

    [Test]
    public async Task WhenTypeIsV7AndCountProvided_ShouldGenerateRightAmountOfData()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V7, 
            Count: 5
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(args.Count);
    }

    [Test]
    public async Task WhenTypeIsV7AndNameProvided_ShouldIgnoreName()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V7,
            Name: "name"
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsNotEqualTo("00000000-0000-0000-0000-000000000000");
        await Assert.That(result.Data.First()).IsNotEqualTo("ffffffff-ffff-ffff-ffff-ffffffffffff");
    }

    [Test]
    public async Task WhenTypeIsV7AndNamespaceProvided_ShouldIgnoreNamespace()
    {
        var args = new UuidGeneratorTool.Args
        (
            Type: UuidGeneratorTool.UuidType.V7,
            Namespace: Guid.NewGuid()
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).Count().IsEqualTo(1);
        await Assert.That(result.Data.First()).IsNotEqualTo("00000000-0000-0000-0000-000000000000");
        await Assert.That(result.Data.First()).IsNotEqualTo("ffffffff-ffff-ffff-ffff-ffffffffffff");
    }
    
    // -------------------------------
    // Formatting tests 

    [Test]
    public async Task WhenCaseIsUppercase_ShouldGenerateUppercaseUuid()
    {
        var args = new UuidGeneratorTool.Args(Type: UuidGeneratorTool.UuidType.V4, Case: UuidGeneratorTool.UuidCase.Uppercase);
        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);
        await Assert.That(result.Data.First()).Matches("^[A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12}$");
    }

    [Test]
    public async Task WhenHyphensIsFalse_ShouldGenerateUuidWithoutHyphens()
    {
        var args = new UuidGeneratorTool.Args(Type: UuidGeneratorTool.UuidType.V4, Hyphens: false);
        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);
        await Assert.That(result.Data.First()).Matches("^[a-f0-9]{32}$");
    }

    [Test]
    public async Task WhenBracketsIsBraces_ShouldGenerateUuidWithBraces()
    {
        var args = new UuidGeneratorTool.Args(Type: UuidGeneratorTool.UuidType.V4, Brackets: UuidGeneratorTool.UuidBrackets.Braces);
        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);
        await Assert.That(result.Data.First()).Matches(@"^\{[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}\}$");
    }

    [Test]
    public async Task WhenBracketsIsParentheses_ShouldGenerateUuidWithParentheses()
    {
        var args = new UuidGeneratorTool.Args(Type: UuidGeneratorTool.UuidType.V4, Brackets: UuidGeneratorTool.UuidBrackets.Parentheses);
        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);
        await Assert.That(result.Data.First()).Matches(@"^\([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}\)$");
    }

    [Test]
    public async Task WhenBracketsIsSquareBrackets_ShouldGenerateUuidWithSquareBrackets()
    {
        var args = new UuidGeneratorTool.Args(Type: UuidGeneratorTool.UuidType.V4, Brackets: UuidGeneratorTool.UuidBrackets.SquareBrackets);
        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);
        await Assert.That(result.Data.First()).Matches(@"^\[[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}\]$");
    }

    [Test]
    public async Task WhenMultipleOptions_ShouldFormatCorrectly()
    {
        var args = new UuidGeneratorTool.Args(
            Type: UuidGeneratorTool.UuidType.V4,
            Hyphens: false,
            Case: UuidGeneratorTool.UuidCase.Uppercase,
            Brackets: UuidGeneratorTool.UuidBrackets.Braces
        );
        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);
        await Assert.That(result.Data.First()).Matches(@"^\{[A-F0-9]{32}\}$");
    }

    // -------------------------------
    // RFC 9562 known answers (namespace DNS, name "www.example.com")

    [Test]
    [Arguments(UuidGeneratorTool.UuidType.V3, "5df41881-3aed-3515-88a7-2f4a814cf09e")]
    [Arguments(UuidGeneratorTool.UuidType.V5, "2ed6657d-e927-568b-95e1-2665a8aea6a2")]
    public async Task WhenTypeIsNameBased_ShouldMatchRfcTestVector(UuidGeneratorTool.UuidType type, string expected)
    {
        var args = new UuidGeneratorTool.Args(
            Type: type,
            Namespace: Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8"),
            Name: "www.example.com"
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data.First()).IsEqualTo(expected);
    }

    [Test]
    [Arguments(UuidGeneratorTool.UuidType.V3, '3')]
    [Arguments(UuidGeneratorTool.UuidType.V5, '5')]
    public async Task WhenTypeIsNameBased_ShouldStampMatchingVersionNibble(UuidGeneratorTool.UuidType type, char version)
    {
        var args = new UuidGeneratorTool.Args(
            Type: type,
            Namespace: Guid.NewGuid(),
            Name: "any-name"
        );

        var result = await new UuidGeneratorTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data.First()[14]).IsEqualTo(version);
    }

    [Test]
    public async Task WhenTypeIsV4_ShouldBuildTheUuidFromTheRandomSource()
    {
        var tool = new UuidGeneratorTool(new Md5Hash(), new ScriptedRandomSource(0xAB));

        var result = await tool.RunAsync(new UuidGeneratorTool.Args(Type: UuidGeneratorTool.UuidType.V4), CancellationToken.None);

        // Sixteen 0xAB bytes with version 4 and the RFC variant stamped in.
        await Assert.That(result.Data.Single()).IsEqualTo("abababab-abab-4bab-abab-abababababab");
    }

    [Test]
    public async Task WhenTypeIsV7_ShouldBuildTheUuidFromTheClockAndTheRandomSource()
    {
        var tool = new UuidGeneratorTool(
            new Md5Hash(),
            new ScriptedRandomSource(0xAB),
            new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));

        var result = await tool.RunAsync(new UuidGeneratorTool.Args(Type: UuidGeneratorTool.UuidType.V7), CancellationToken.None);

        // "019b76da-a800" is 1767225600000 ms, 2026-01-01T00:00:00Z; the rest is 0xAB bytes with version 7 and the variant.
        await Assert.That(result.Data.Single()).IsEqualTo("019b76da-a800-7bab-abab-abababababab");
    }

    [Test]
    [Arguments(UuidGeneratorTool.UuidType.V4)]
    [Arguments(UuidGeneratorTool.UuidType.V7)]
    public async Task WhenTheRandomBytesAreZero_ShouldStillStampTheRfcVariant(UuidGeneratorTool.UuidType type)
    {
        // The variant bits must read 10 whatever the random byte under them. V7 used to leave them
        // random, so this came out as '0' for V7.
        var tool = new UuidGeneratorTool(new Md5Hash(), new ScriptedRandomSource(0x00));

        var result = await tool.RunAsync(new UuidGeneratorTool.Args(Type: type), CancellationToken.None);

        await Assert.That(result.Data.Single()[19]).IsEqualTo('8');
    }

    [Test]
    public async Task WhenTypeIsV7_EveryUuidShouldCarryTheRfcVariant()
    {
        var result = await new UuidGeneratorTool(new Md5Hash())
            .RunAsync(new UuidGeneratorTool.Args(Type: UuidGeneratorTool.UuidType.V7, Count: 500), CancellationToken.None);

        var wrongVariant = result.Data.Where(it => !"89ab".Contains(it[19])).ToArray();

        await Assert.That(wrongVariant).IsEmpty();
    }
}
