using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "ip-subnet",
    Title = "IP Subnet Calculator",
    Description =
        """
        Calculates an IP network: its address, mask, broadcast address, host range and size, for IPv4
        and IPv6 alike. Given a range of addresses, it also lists the CIDR blocks that cover it.

        The input is a network in CIDR notation, such as 192.168.1.0/24 or 2001:db8::/32, where host bits
        may be set and are cleared; an IPv4 network with a dotted mask, such as 10.0.0.0/255.0.0.0; a single
        address, read as a network of one; or a range written start-end, such as 10.0.0.1-10.0.0.100. For
        a range, the result describes the smallest network holding all of it, and Blocks lists the fewest
        CIDR blocks that cover exactly the range.

        An IPv4 address is four decimal numbers from 0 to 255: a leading zero does not make a number
        octal, and shortened forms such as 10.1 are refused. The first and last addresses of an IPv4
        network are its network and broadcast addresses, so they are not hosts - except in a /31, where
        both are (RFC 3021), and a /32, which is a single host. IPv6 has no broadcast address, and every
        address of the network counts as a host. Counts are decimal text, because an IPv6 network can
        hold more addresses than a 64-bit number.

        The class is the one an IPv4 network address had before CIDR, read from its first number: A up to
        127, B up to 191, C up to 223, D for multicast and E reserved. It no longer decides how big a
        network is, and IPv6 has none.
        """,
    Usage =
        "Use this instead of doing subnet arithmetic by hand: masks, broadcast addresses, host counts and the CIDR blocks that make up a range are easy to get off by one. It only calculates - it does not look an address up, so it cannot say who owns it or whether it is reachable.",
    Aliases = ["subnet"],
    Keywords = [Keyword.Ip, Keyword.Network],
    Categories = [Category.Network]
)]
public sealed class IpSubnetTool : ToolBase<IpSubnetTool.Args, IpSubnetTool.Result>
{
    private const int Ipv4Bits = 32;
    private const int Ipv6Bits = 128;

    protected override Result Execute(Args args)
    {
        if (string.IsNullOrWhiteSpace(args.Input))
        {
            throw new ToolException(ErrorCode.TextEmpty, nameof(Args.Input));
        }

        var range = ParseRange(args.Input.Trim())
                    ?? throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Input));

        // The smallest network holding the whole range: the bits the first and last addresses share.
        var differing = range.First ^ range.Last;
        var hostBits = differing == UInt128.Zero ? 0 : Ipv6Bits - (int)UInt128.LeadingZeroCount(differing);
        var hostMask = HostMask(hostBits);
        var network = range.First & ~hostMask;
        var lastAddress = network | hostMask;
        var prefix = range.Width - hostBits;
        var isIpv4 = range.Width == Ipv4Bits;

        var addressCount = BigInteger.One << hostBits;
        var (firstHost, lastHost, hostCount) = isIpv4 && hostBits >= 2
            ? (network + 1, lastAddress - 1, addressCount - 2)
            : (network, lastAddress, addressCount);

        return new Result(
            Version: isIpv4 ? 4 : 6,
            Network: $"{Format(network, range.Width)}/{prefix.ToString(CultureInfo.InvariantCulture)}",
            NetworkAddress: Format(network, range.Width),
            PrefixLength: prefix,
            SubnetMask: Format(HostMask(range.Width) & ~hostMask, range.Width),
            WildcardMask: Format(hostMask, range.Width),
            BroadcastAddress: isIpv4 ? Format(lastAddress, range.Width) : null,
            IpClass: isIpv4 ? ClassOf((uint)network) : null,
            FirstHost: Format(firstHost, range.Width),
            LastHost: Format(lastHost, range.Width),
            HostCount: hostCount.ToString(CultureInfo.InvariantCulture),
            AddressCount: addressCount.ToString(CultureInfo.InvariantCulture),
            Blocks: Blocks(range)
        );
    }

    private static AddressRange? ParseRange(string input)
    {
        var dash = input.IndexOf('-');
        if (dash >= 0)
        {
            var start = ParseAddress(input[..dash].Trim());
            var end = ParseAddress(input[(dash + 1)..].Trim());
            if (start is null || end is null || start.Value.Width != end.Value.Width || start.Value.Value > end.Value.Value)
            {
                return null;
            }

            return new AddressRange(start.Value.Value, end.Value.Value, start.Value.Width);
        }

        var slash = input.IndexOf('/');
        var address = ParseAddress((slash >= 0 ? input[..slash] : input).Trim());
        if (address is null)
        {
            return null;
        }

        var width = address.Value.Width;
        var prefix = slash >= 0 ? ParsePrefix(input[(slash + 1)..].Trim(), width) : width;
        if (prefix is null)
        {
            return null;
        }

        var hostMask = HostMask(width - prefix.Value);
        var network = address.Value.Value & ~hostMask;

        return new AddressRange(network, network | hostMask, width);
    }

    private static Address? ParseAddress(string text)
    {
        if (text.Contains(':'))
        {
            // A zone index (fe80::1%eth0) names an interface on one machine, not part of a network.
            if (text.Contains('%') || !IPAddress.TryParse(text, out var ip) || ip.AddressFamily != AddressFamily.InterNetworkV6)
            {
                return null;
            }

            return new Address(BinaryPrimitives.ReadUInt128BigEndian(ip.GetAddressBytes()), Ipv6Bits);
        }

        // Not IPAddress.TryParse: it reads 10.1 as 10.0.0.1 and 010.0.0.1 as octal.
        var parts = text.Split('.');
        if (parts.Length != 4)
        {
            return null;
        }

        uint value = 0;
        foreach (var part in parts)
        {
            if (part.Length is 0 or > 3 || !part.All(char.IsAsciiDigit))
            {
                return null;
            }

            var octet = uint.Parse(part, CultureInfo.InvariantCulture);
            if (octet > 255)
            {
                return null;
            }

            value = value << 8 | octet;
        }

        return new Address(value, Ipv4Bits);
    }

    private static int? ParsePrefix(string text, int width)
    {
        if (text.Length is > 0 and <= 3 && text.All(char.IsAsciiDigit))
        {
            var prefix = int.Parse(text, CultureInfo.InvariantCulture);
            return prefix <= width ? prefix : null;
        }

        // A dotted IPv4 mask, which must be ones followed by zeros.
        if (width == Ipv4Bits && ParseAddress(text) is { Width: Ipv4Bits } mask)
        {
            var host = ~(uint)mask.Value;
            return (host & (host + 1)) == 0 ? Ipv4Bits - BitOperations.PopCount(host) : null;
        }

        return null;
    }

    // Largest block that starts at the next uncovered address and still ends inside the range, until the range is covered.
    private static List<string> Blocks(AddressRange range)
    {
        var blocks = new List<string>();
        var start = range.First;
        while (true)
        {
            var hostBits = start == UInt128.Zero
                ? range.Width
                : Math.Min(range.Width, (int)UInt128.TrailingZeroCount(start));
            while (HostMask(hostBits) > range.Last - start)
            {
                hostBits--;
            }

            blocks.Add($"{Format(start, range.Width)}/{(range.Width - hostBits).ToString(CultureInfo.InvariantCulture)}");

            var end = start + HostMask(hostBits);
            if (end == range.Last)
            {
                return blocks;
            }

            start = end + 1;
        }
    }

    // The low `bits` bits set.
    private static UInt128 HostMask(int bits) => bits switch
    {
        0 => UInt128.Zero,
        Ipv6Bits => UInt128.MaxValue,
        _ => (UInt128.One << bits) - 1
    };

    private static string Format(UInt128 value, int width)
    {
        if (width == Ipv4Bits)
        {
            var address = (uint)value;
            return string.Create(CultureInfo.InvariantCulture,
                $"{address >> 24}.{address >> 16 & 0xFF}.{address >> 8 & 0xFF}.{address & 0xFF}");
        }

        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteUInt128BigEndian(bytes, value);
        return new IPAddress(bytes).ToString();
    }

    private static string ClassOf(uint address) => (address >> 24) switch
    {
        < 128 => "A",
        < 192 => "B",
        < 224 => "C",
        < 240 => "D",
        _ => "E"
    };

    private readonly record struct Address(UInt128 Value, int Width);

    private readonly record struct AddressRange(UInt128 First, UInt128 Last, int Width);

    public sealed record Args(
        [property: PipeInput,
                   Description("A network in CIDR notation (192.168.1.0/24, 2001:db8::/32), an IPv4 network with a dotted mask (10.0.0.0/255.0.0.0), a single address, or a range written start-end (10.0.0.1-10.0.0.100)")]
        string? Input
    );

    public sealed record Result(
        [property: Description("4 or 6")]
        int Version,
        [property: Description("The network in CIDR notation; for a range, the smallest network holding all of it")]
        string Network,
        [property: Description("The first address of the network, with every host bit cleared")]
        string NetworkAddress,
        [property: Description("How many leading bits of an address identify the network")]
        int PrefixLength,
        [property: Description("The prefix as a mask: network bits set, host bits cleared")]
        string SubnetMask,
        [property: Description("The inverse of the subnet mask, as access control lists take it: host bits set")]
        string WildcardMask,
        [property: Description("The last address of an IPv4 network; none for IPv6, which has no broadcast")]
        string? BroadcastAddress,
        [property: DisplayName("IP Class"),
                   Description("The pre-CIDR class of an IPv4 network address - A, B, C, D (multicast) or E (reserved); none for IPv6")]
        string? IpClass,
        [property: Description("The first address a host can use")]
        string FirstHost,
        [property: Description("The last address a host can use")]
        string LastHost,
        [property: Description("How many addresses hosts can use, as decimal text")]
        string HostCount,
        [property: Description("How many addresses the network holds, as decimal text")]
        string AddressCount,
        [property: Description("The fewest CIDR blocks that cover exactly the input: for a range, its blocks in order; for a network or an address, that network alone")]
        IList<string> Blocks
    ) : ToolResult
    {
        public Result() : this(0, string.Empty, string.Empty, 0, string.Empty, string.Empty, null, null,
            string.Empty, string.Empty, string.Empty, string.Empty, [])
        {
        }
    }
}
