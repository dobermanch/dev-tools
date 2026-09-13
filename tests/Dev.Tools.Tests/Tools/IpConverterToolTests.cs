using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class IpConverterToolTests
{
    private static Task<IpConverterTool.Result> RunAsync(string? address)
        => new IpConverterTool().RunAsync(new IpConverterTool.Args(address), CancellationToken.None);

    // Every field on its own line, so a failing test shows the whole result.
    private static string Describe(IpConverterTool.Result result)
        => string.Join("\n", result.Version, result.Ipv4 ?? "-", result.Decimal, result.Hexadecimal, result.Binary,
            result.Ipv6, result.Ipv6Expanded);

    [Test]
    [Arguments("192.168.1.1")]
    [Arguments(" 192.168.001.001 ")]
    [Arguments("3232235777")]
    [Arguments("0xC0A80101")]
    [Arguments("0Xc0a80101")]
    [Arguments("0b11000000101010000000000100000001")]
    public async Task ShouldConvertAnIpv4AddressFromEveryNotation(string address)
    {
        var result = await RunAsync(address);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(Describe(result)).IsEqualTo(string.Join("\n",
            "4",
            "192.168.1.1",
            "3232235777",
            "0xC0A80101",
            "0b11000000101010000000000100000001",
            "::ffff:192.168.1.1",
            "0000:0000:0000:0000:0000:ffff:c0a8:0101"));
    }

    [Test]
    public async Task ShouldConvertAnIpv6Address()
    {
        var result = await RunAsync("2001:DB8:0:0::1");

        await Assert.That(Describe(result)).IsEqualTo(string.Join("\n",
            "6",
            "-",
            "42540766411282592856903984951653826561",
            "0x20010DB8000000000000000000000001",
            "0b" + "0010000000000001" + "0000110110111000" + new string('0', 95) + "1",
            "2001:db8::1",
            "2001:0db8:0000:0000:0000:0000:0000:0001"));
    }

    [Test]
    public async Task ShouldGiveTheIpv4AddressAnIpv4MappedAddressCarries()
    {
        var result = await RunAsync("::ffff:10.0.0.1");

        await Assert.That(result.Version).IsEqualTo(6);
        await Assert.That(result.Ipv4).IsEqualTo("10.0.0.1");
        await Assert.That(result.Hexadecimal).IsEqualTo("0x00000000000000000000FFFF0A000001");
        await Assert.That(result.Ipv6).IsEqualTo("::ffff:10.0.0.1");
    }

    [Test]
    [Arguments("0", 4, "0.0.0.0", "::ffff:0.0.0.0")]
    [Arguments("4294967295", 4, "255.255.255.255", "::ffff:255.255.255.255")]
    [Arguments("4294967296", 6, null, "::1:0:0")]
    [Arguments("0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", 6, null, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
    [Arguments("340282366920938463463374607431768211455", 6, null, "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
    public async Task ShouldReadANumberThatFitsIn32BitsAsIpv4(string address, int version, string? ipv4, string ipv6)
    {
        var result = await RunAsync(address);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Version).IsEqualTo(version);
        await Assert.That(result.Ipv4).IsEqualTo(ipv4);
        await Assert.That(result.Ipv6).IsEqualTo(ipv6);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" ")]
    public async Task ShouldReturnError_WhenAddressIsEmpty(string? address)
    {
        var result = await RunAsync(address);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.TextEmpty);
    }

    [Test]
    [Arguments("abc")]
    [Arguments("10.1")]
    [Arguments("256.0.0.1")]
    [Arguments("-1")]
    [Arguments("1.2.3.4/24")]
    [Arguments("fe80::1%eth0")]
    [Arguments("0x")]
    [Arguments("0xG1")]
    [Arguments("0x 1")]
    [Arguments("0x1FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF")]
    [Arguments("0b102")]
    [Arguments("340282366920938463463374607431768211456")]
    public async Task ShouldReturnError_WhenAddressIsNotValid(string address)
    {
        var result = await RunAsync(address);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.InputNotValid);
        await Assert.That(result.Errors[0].Target).IsEqualTo(nameof(IpConverterTool.Args.Address));
    }
}
