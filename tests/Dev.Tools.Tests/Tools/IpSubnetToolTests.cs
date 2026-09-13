using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class IpSubnetToolTests
{
    private static Task<IpSubnetTool.Result> RunAsync(string? input)
        => new IpSubnetTool().RunAsync(new IpSubnetTool.Args(input), CancellationToken.None);

    // "network mask wildcard broadcast first-last hosts/addresses", so a failing test shows every field.
    private static string Describe(IpSubnetTool.Result result)
        => $"{result.Network} {result.SubnetMask} {result.WildcardMask} {result.BroadcastAddress ?? "-"} "
           + $"{result.FirstHost}-{result.LastHost} {result.HostCount}/{result.AddressCount}";

    [Test]
    [Arguments("192.168.1.0/24", "192.168.1.0/24 255.255.255.0 0.0.0.255 192.168.1.255 192.168.1.1-192.168.1.254 254/256")]
    [Arguments("192.168.1.10/24", "192.168.1.0/24 255.255.255.0 0.0.0.255 192.168.1.255 192.168.1.1-192.168.1.254 254/256")]
    [Arguments("172.16.5.4/255.240.0.0", "172.16.0.0/12 255.240.0.0 0.15.255.255 172.31.255.255 172.16.0.1-172.31.255.254 1048574/1048576")]
    [Arguments("10.0.0.0/30", "10.0.0.0/30 255.255.255.252 0.0.0.3 10.0.0.3 10.0.0.1-10.0.0.2 2/4")]
    [Arguments("10.0.0.0/31", "10.0.0.0/31 255.255.255.254 0.0.0.1 10.0.0.1 10.0.0.0-10.0.0.1 2/2")]
    [Arguments("8.8.8.8", "8.8.8.8/32 255.255.255.255 0.0.0.0 8.8.8.8 8.8.8.8-8.8.8.8 1/1")]
    [Arguments("0.0.0.0/0", "0.0.0.0/0 0.0.0.0 255.255.255.255 255.255.255.255 0.0.0.1-255.255.255.254 4294967294/4294967296")]
    [Arguments(" 010.000.001.255 / 16 ", "10.0.0.0/16 255.255.0.0 0.0.255.255 10.0.255.255 10.0.0.1-10.0.255.254 65534/65536")]
    public async Task ShouldCalculateAnIpv4Network(string input, string expected)
    {
        var result = await RunAsync(input);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Version).IsEqualTo(4);
        await Assert.That(Describe(result)).IsEqualTo(expected);
        await Assert.That(result.Blocks).IsEquivalentTo([result.Network]);
    }

    [Test]
    [Arguments("2001:db8::1/64", "2001:db8::/64 ffff:ffff:ffff:ffff:: ::ffff:ffff:ffff:ffff - 2001:db8::-2001:db8::ffff:ffff:ffff:ffff 18446744073709551616/18446744073709551616")]
    [Arguments("::1", "::1/128 ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff :: - ::1-::1 1/1")]
    [Arguments("::/0", "::/0 :: ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff - ::-ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff 340282366920938463463374607431768211456/340282366920938463463374607431768211456")]
    public async Task ShouldCalculateAnIpv6Network(string input, string expected)
    {
        var result = await RunAsync(input);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Version).IsEqualTo(6);
        await Assert.That(Describe(result)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("192.168.3.0/24", "C")]
    [Arguments("10.20.0.0/8", "A")]
    [Arguments("127.0.0.1", "A")]
    [Arguments("128.0.0.0/16", "B")]
    [Arguments("191.255.0.0/16", "B")]
    [Arguments("223.255.255.0/24", "C")]
    [Arguments("224.0.0.1", "D")]
    [Arguments("240.0.0.0/4", "E")]
    [Arguments("2001:db8::/32", null)]
    public async Task ShouldGiveTheClassOfTheNetworkAddress(string input, string? ipClass)
    {
        var result = await RunAsync(input);

        await Assert.That(result.IpClass).IsEqualTo(ipClass);
    }

    [Test]
    public async Task ShouldCoverARangeWithTheFewestBlocks()
    {
        var result = await RunAsync("10.0.0.1-10.0.0.100");

        await Assert.That(result.Network).IsEqualTo("10.0.0.0/25");
        await Assert.That(string.Join(" ", result.Blocks)).IsEqualTo(
            "10.0.0.1/32 10.0.0.2/31 10.0.0.4/30 10.0.0.8/29 10.0.0.16/28 10.0.0.32/27 10.0.0.64/27 10.0.0.96/30 10.0.0.100/32");
    }

    [Test]
    [Arguments("0.0.0.0-255.255.255.255", "0.0.0.0/0", "0.0.0.0/0")]
    [Arguments("::-ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff", "::/0", "::/0")]
    [Arguments("192.168.0.0 - 192.168.1.255", "192.168.0.0/23", "192.168.0.0/23")]
    [Arguments("10.0.0.5-10.0.0.5", "10.0.0.5/32", "10.0.0.5/32")]
    [Arguments("2001:db8::ffff-2001:db8::1:0", "2001:db8::/111", "2001:db8::ffff/128 2001:db8::1:0/128")]
    public async Task ShouldCoverARangeReachingTheEdges(string input, string network, string blocks)
    {
        var result = await RunAsync(input);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Network).IsEqualTo(network);
        await Assert.That(string.Join(" ", result.Blocks)).IsEqualTo(blocks);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("  ")]
    public async Task ShouldReturnError_WhenInputIsEmpty(string? input)
    {
        var result = await RunAsync(input);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.TextEmpty);
    }

    [Test]
    [Arguments("abc")]
    [Arguments("10.1")]
    [Arguments("300.1.1.1")]
    [Arguments("1.2.3.4.5")]
    [Arguments("1.2.3.-4")]
    [Arguments("1.2.3.4/33")]
    [Arguments("1.2.3.4/")]
    [Arguments("1.2.3.4/255.0.255.0")]
    [Arguments("::1/129")]
    [Arguments("2001:db8::/ffff::")]
    [Arguments("fe80::1%eth0")]
    [Arguments("1.2.3.4-")]
    [Arguments("10.0.0.9-10.0.0.1")]
    [Arguments("10.0.0.1-::1")]
    [Arguments("10.0.0.0/24-10.0.1.0/24")]
    public async Task ShouldReturnError_WhenInputIsNotValid(string input)
    {
        var result = await RunAsync(input);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.InputNotValid);
        await Assert.That(result.Errors[0].Target).IsEqualTo(nameof(IpSubnetTool.Args.Input));
    }
}
