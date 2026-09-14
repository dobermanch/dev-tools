using Dev.Tools.Tests.Helpers;
using Dev.Tools.Tools;
using static Dev.Tools.Tools.BcryptTool;

namespace Dev.Tools.Tests.Tools;

public class BcryptToolTests
{
    private static Task<Result> RunAsync(Args args, BcryptTool? tool = null)
        => (tool ?? new BcryptTool()).RunAsync(args, CancellationToken.None);

    private static Task<Result> VerifyAsync(string password, string hash)
        => RunAsync(new Args(password, BcryptOperation.Verify, hash));

    [Test]
    public async Task ShouldHashWithTheSaltFromTheRandomSource()
    {
        var tool = new BcryptTool(new ScriptedRandomSource(0x00));

        var first = await RunAsync(new Args("password", Cost: 4), tool);
        var second = await RunAsync(new Args("password", Cost: 4), tool);

        await Assert.That(first.HasErrors).IsFalse();
        await Assert.That(first.Hash).StartsWith("$2b$04$......................");
        await Assert.That(first.Hash.Length).IsEqualTo(60);
        await Assert.That(first.Version).IsEqualTo("2b");
        await Assert.That(first.Cost).IsEqualTo(4);
        await Assert.That(first.Salt).IsEqualTo(new string('.', 22));
        await Assert.That(first.Matches).IsNull();
        // Same salt, same password: the same hash, which is what the seam is for.
        await Assert.That(second.Hash).IsEqualTo(first.Hash);
    }

    [Test]
    public async Task ShouldWriteTheSaltInBcryptsAlphabet()
    {
        // 16 bytes of 0xFF: 21 full six-bit groups of ones, and a last character holding the final two bits.
        var result = await RunAsync(new Args("password", Cost: 4), new BcryptTool(new ScriptedRandomSource(0xFF)));

        await Assert.That(result.Salt).IsEqualTo(new string('9', 21) + "u");
    }

    [Test]
    public async Task ShouldVerifyWhatItHashed()
    {
        var hashed = await RunAsync(new Args("correct horse", Cost: 4));

        var right = await VerifyAsync("correct horse", hashed.Hash);
        var wrong = await VerifyAsync("battery staple", hashed.Hash);

        await Assert.That(right.Matches).IsEqualTo(true);
        await Assert.That(wrong.Matches).IsEqualTo(false);
    }

    [Test]
    public async Task ShouldGiveDifferentHashesForTheSamePassword_WithTheDefaultRandomSource()
    {
        var first = await RunAsync(new Args("password", Cost: 4));
        var second = await RunAsync(new Args("password", Cost: 4));

        await Assert.That(first.Hash).IsNotEqualTo(second.Hash);
    }

    [Test]
    // The jBCrypt and OpenBSD test vectors.
    [Arguments("", "$2a$06$DCq7YPn5Rq63x1Lad4cll.TV4S6ytwfsfvkgY8jIucDrjc8deX1s.")]
    [Arguments("a", "$2a$06$m0CrhHm10qJ3lXRY.5zDGO3rS2KdeeWLuGmsfGlMfOxih58VYVfxe")]
    [Arguments("abc", "$2a$06$If6bvum7DFjUnE9p2uDeDu0YHzrHM6tf.iqN8.yx.jNN1ILEf7h0i")]
    [Arguments("abcdefghijklmnopqrstuvwxyz", "$2a$06$.rCVZVOThsIa97pEDOxvGuRRgzG64bvtJ0938xuqzv18d3ZpQhstC")]
    public async Task ShouldMatchKnownHashes(string password, string hash)
    {
        // An empty password is refused by the tool, so the empty vector is checked through the library it wraps.
        if (password.Length == 0)
        {
            await Assert.That(BCrypt.Net.BCrypt.Verify(password, hash)).IsTrue();
            return;
        }

        var result = await VerifyAsync(password, hash);

        await Assert.That(result.Matches).IsEqualTo(true);
        await Assert.That(result.Version).IsEqualTo("2a");
        await Assert.That(result.Cost).IsEqualTo(6);
    }

    [Test]
    [Arguments("$2b$06$If6bvum7DFjUnE9p2uDeDu0YHzrHM6tf.iqN8.yx.jNN1ILEf7h0i", "2b")]
    [Arguments("$2y$06$If6bvum7DFjUnE9p2uDeDu0YHzrHM6tf.iqN8.yx.jNN1ILEf7h0i", "2y")]
    [Arguments("  $2y$06$If6bvum7DFjUnE9p2uDeDu0YHzrHM6tf.iqN8.yx.jNN1ILEf7h0i\n", "2y")]
    public async Task ShouldVerifyTheVersionsOtherLibrariesWrite(string hash, string version)
    {
        var result = await VerifyAsync("abc", hash);

        await Assert.That(result.Matches).IsEqualTo(true);
        await Assert.That(result.Version).IsEqualTo(version);
        await Assert.That(result.Salt).IsEqualTo("If6bvum7DFjUnE9p2uDeDu");
    }

    [Test]
    [Arguments(72, false)]
    [Arguments(73, true)]
    public async Task ShouldFlagAPasswordLongerThanBcryptReads(int length, bool truncated)
    {
        var result = await RunAsync(new Args(new string('x', length), Cost: 4));

        await Assert.That(result.PasswordTruncated).IsEqualTo(truncated);
    }

    [Test]
    public async Task ShouldCountThePasswordInUtf8Bytes()
    {
        // A Cyrillic letter is 2 bytes of UTF-8: 37 of them are only 37 characters, but 74 bytes.
        var result = await RunAsync(new Args(new string('ж', 37), Cost: 4));

        await Assert.That(result.PasswordTruncated).IsTrue();
    }

    [Test]
    [Arguments(0, 4)]
    [Arguments(3, 4)]
    public async Task ShouldHoldTheCostAtTheMinimum(int cost, int expected)
    {
        var result = await RunAsync(new Args("password", Cost: cost));

        await Assert.That(result.Cost).IsEqualTo(expected);
        await Assert.That(result.Hash).StartsWith("$2b$04$");
    }

    [Test]
    public async Task ShouldReportEveryMissingArgument()
    {
        var result = await RunAsync(new Args("", BcryptOperation.Verify));

        await Assert.That(result.Errors.Select(it => $"{it.Code} {it.Target}"))
            .IsEquivalentTo(["TextEmpty Password", "TextEmpty Hash"]);
    }

    [Test]
    [Arguments("not-a-hash")]
    [Arguments("$2b$06$short")]
    [Arguments("$1$06$If6bvum7DFjUnE9p2uDeDu0YHzrHM6tf.iqN8.yx.jNN1ILEf7h0i")]
    [Arguments("$2b$03$If6bvum7DFjUnE9p2uDeDu0YHzrHM6tf.iqN8.yx.jNN1ILEf7h0i")]
    [Arguments("$2b$16$If6bvum7DFjUnE9p2uDeDu0YHzrHM6tf.iqN8.yx.jNN1ILEf7h0i")]
    [Arguments("$2b$31$If6bvum7DFjUnE9p2uDeDu0YHzrHM6tf.iqN8.yx.jNN1ILEf7h0i")]
    public async Task ShouldRefuseAHashItCannotOrWillNotCheck(string hash)
    {
        var result = await VerifyAsync("abc", hash);

        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.InputNotValid);
        await Assert.That(result.Errors[0].Target).IsEqualTo(nameof(Args.Hash));
    }
}
