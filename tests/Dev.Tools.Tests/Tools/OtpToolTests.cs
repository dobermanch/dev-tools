using Dev.Tools.Tests.Helpers;
using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class OtpToolTests
{
    // The RFC seeds "12345678901234567890" repeated to 20, 32 and 64 bytes, in Base32.
    private const string Sha1Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
    private const string Sha256Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZA====";
    private const string Sha512Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNA=";

    private static OtpTool At(long unixSeconds) => new(time: new FixedTimeProvider(DateTimeOffset.FromUnixTimeSeconds(unixSeconds)));

    private static Task<OtpTool.Result> RunAsync(OtpTool.Args args, OtpTool? tool = null)
        => (tool ?? At(0)).RunAsync(args, CancellationToken.None);

    [Test]
    [Arguments(0, "755224")]
    [Arguments(1, "287082")]
    [Arguments(2, "359152")]
    [Arguments(3, "969429")]
    [Arguments(4, "338314")]
    [Arguments(5, "254676")]
    [Arguments(6, "287922")]
    [Arguments(7, "162583")]
    [Arguments(8, "399871")]
    [Arguments(9, "520489")]
    public async Task ShouldMatchTheRfc4226HotpVectors(long counter, string expected)
    {
        var result = await RunAsync(new OtpTool.Args(Sha1Secret, OtpTool.OtpType.Hotp, Counter: counter));

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Code).IsEqualTo(expected);
        await Assert.That(result.Counter).IsEqualTo(counter);
        await Assert.That(result.SecondsRemaining).IsNull();
    }

    [Test]
    [Arguments(59L, OtpTool.OtpAlgorithm.Sha1, "94287082")]
    [Arguments(59L, OtpTool.OtpAlgorithm.Sha256, "46119246")]
    [Arguments(59L, OtpTool.OtpAlgorithm.Sha512, "90693936")]
    [Arguments(1111111109L, OtpTool.OtpAlgorithm.Sha1, "07081804")]
    [Arguments(1111111109L, OtpTool.OtpAlgorithm.Sha256, "68084774")]
    [Arguments(1111111109L, OtpTool.OtpAlgorithm.Sha512, "25091201")]
    [Arguments(1111111111L, OtpTool.OtpAlgorithm.Sha1, "14050471")]
    [Arguments(1111111111L, OtpTool.OtpAlgorithm.Sha256, "67062674")]
    [Arguments(1111111111L, OtpTool.OtpAlgorithm.Sha512, "99943326")]
    [Arguments(1234567890L, OtpTool.OtpAlgorithm.Sha1, "89005924")]
    [Arguments(1234567890L, OtpTool.OtpAlgorithm.Sha256, "91819424")]
    [Arguments(1234567890L, OtpTool.OtpAlgorithm.Sha512, "93441116")]
    [Arguments(2000000000L, OtpTool.OtpAlgorithm.Sha1, "69279037")]
    [Arguments(2000000000L, OtpTool.OtpAlgorithm.Sha256, "90698825")]
    [Arguments(2000000000L, OtpTool.OtpAlgorithm.Sha512, "38618901")]
    [Arguments(20000000000L, OtpTool.OtpAlgorithm.Sha1, "65353130")]
    [Arguments(20000000000L, OtpTool.OtpAlgorithm.Sha256, "77737706")]
    [Arguments(20000000000L, OtpTool.OtpAlgorithm.Sha512, "47863826")]
    public async Task ShouldMatchTheRfc6238TotpVectors(long unixSeconds, OtpTool.OtpAlgorithm algorithm, string expected)
    {
        var secret = algorithm switch
        {
            OtpTool.OtpAlgorithm.Sha256 => Sha256Secret,
            OtpTool.OtpAlgorithm.Sha512 => Sha512Secret,
            _ => Sha1Secret
        };

        var result = await RunAsync(new OtpTool.Args(secret, Algorithm: algorithm, Digits: 8), At(unixSeconds));

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Code).IsEqualTo(expected);
    }

    [Test]
    public async Task ShouldGiveTheNeighbouringCodesAndTheSecondsLeft()
    {
        var result = await RunAsync(new OtpTool.Args(Sha1Secret), At(1111111111));

        var hotp = await RunAsync(new OtpTool.Args(Sha1Secret, OtpTool.OtpType.Hotp, Counter: 37037036));
        await Assert.That(result.Counter).IsEqualTo(37037037);
        await Assert.That(result.SecondsRemaining).IsEqualTo(29);
        await Assert.That(result.PreviousCode).IsEqualTo(hotp.Code);
        await Assert.That(result.Code).IsEqualTo(hotp.NextCode);
    }

    [Test]
    public async Task ShouldGiveNoPreviousCode_AtCounterZero()
    {
        var result = await RunAsync(new OtpTool.Args(Sha1Secret, OtpTool.OtpType.Hotp));

        await Assert.That(result.PreviousCode).IsNull();
        await Assert.That(result.NextCode).IsEqualTo("287082");
    }

    [Test]
    public async Task ShouldGenerateASecret_WhenNoneIsGiven()
    {
        var tool = new OtpTool(new ScriptedRandomSource(0xFF), new FixedTimeProvider(DateTimeOffset.UnixEpoch));

        var result = await RunAsync(new OtpTool.Args(null), tool);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Secret).IsEqualTo(new string('7', 32));
        await Assert.That(result.SecretHex).IsEqualTo(new string('f', 40));
    }

    [Test]
    [Arguments("gezd gnbv-gy3t qojq GEZDGNBVGY3TQOJQ")]
    [Arguments("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ====")]
    public async Task ShouldReadTheSecretHoweverItIsWritten(string secret)
    {
        var result = await RunAsync(new OtpTool.Args(secret, OtpTool.OtpType.Hotp));

        await Assert.That(result.Secret).IsEqualTo(Sha1Secret);
        await Assert.That(result.SecretHex).IsEqualTo(Convert.ToHexStringLower("12345678901234567890"u8));
        await Assert.That(result.Code).IsEqualTo("755224");
    }

    [Test]
    [Arguments("GEZD1GNB")]
    [Arguments("A")]
    [Arguments("GE=ZDGNBV")]
    [Arguments("ЖЖЖЖЖЖЖЖ")]
    public async Task ShouldReturnError_WhenTheSecretIsNotBase32(string secret)
    {
        var result = await RunAsync(new OtpTool.Args(secret));

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.InputNotValid);
        await Assert.That(result.Errors[0].Target).IsEqualTo(nameof(OtpTool.Args.Secret));
    }

    [Test]
    [Arguments("14050471", 1, true, 0)]
    [Arguments("1405 0471", 0, true, 0)]
    [Arguments("07081804", 1, true, -1)]
    [Arguments("07081804", 0, false, null)]
    [Arguments("00000000", 1, false, null)]
    [Arguments("1405047", 1, false, null)]
    public async Task ShouldCheckATotpCodeWithinTheWindow(string code, int window, bool valid, int? offset)
    {
        var result = await RunAsync(new OtpTool.Args(Sha1Secret, Digits: 8, Code: code, Window: window), At(1111111111));

        await Assert.That(result.IsValid).IsEqualTo(valid);
        await Assert.That(result.MatchOffset).IsEqualTo(offset);
    }

    [Test]
    [Arguments("254676", 2, true, 2)]
    [Arguments("254676", 1, false, null)]
    [Arguments("359152", 5, false, null)]
    public async Task ShouldCheckAnHotpCodeOnlyAhead(string code, int window, bool valid, int? offset)
    {
        var result = await RunAsync(new OtpTool.Args(Sha1Secret, OtpTool.OtpType.Hotp, Counter: 3, Code: code, Window: window));

        await Assert.That(result.IsValid).IsEqualTo(valid);
        await Assert.That(result.MatchOffset).IsEqualTo(offset);
    }

    [Test]
    public async Task ShouldCheckNothing_WhenNoCodeIsGiven()
    {
        var result = await RunAsync(new OtpTool.Args(Sha1Secret));

        await Assert.That(result.IsValid).IsNull();
        await Assert.That(result.MatchOffset).IsNull();
    }

    [Test]
    [Arguments(4, 6)]
    [Arguments(12, 10)]
    public async Task ShouldClampTheDigits(int digits, int expected)
    {
        var result = await RunAsync(new OtpTool.Args(Sha1Secret, Digits: digits));

        await Assert.That(result.Code.Length).IsEqualTo(expected);
    }

    [Test]
    public async Task ShouldBuildTheTotpUri()
    {
        var result = await RunAsync(new OtpTool.Args("JBSWY3DPEHPK3PXP", Issuer: "ACME Co", AccountName: "jane@example.com"));

        await Assert.That(result.Uri).IsEqualTo(
            "otpauth://totp/ACME%20Co:jane%40example.com?secret=JBSWY3DPEHPK3PXP&issuer=ACME%20Co&algorithm=SHA1&digits=6&period=30");
    }

    [Test]
    public async Task ShouldBuildTheHotpUri()
    {
        var result = await RunAsync(new OtpTool.Args("JBSWY3DPEHPK3PXP", OtpTool.OtpType.Hotp, OtpTool.OtpAlgorithm.Sha256, 8, Counter: 5, AccountName: "jane"));

        await Assert.That(result.Uri).IsEqualTo("otpauth://hotp/jane?secret=JBSWY3DPEHPK3PXP&algorithm=SHA256&digits=8&counter=5");
    }
}
