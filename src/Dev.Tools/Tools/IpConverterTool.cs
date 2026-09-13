using System.Globalization;
using Dev.Tools.Networking;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "ip-converter",
    Title = "IP Address Converter",
    Description =
        """
        Converts an IP address between its notations: dotted IPv4, IPv6, and a single decimal,
        hexadecimal or binary number.

        The input is an IPv4 address (192.168.1.1), an IPv6 address (2001:db8::1), or the address as a
        number: decimal digits, hexadecimal after 0x, or binary after 0b. A number that fits in 32 bits is
        read as an IPv4 address and a larger one as IPv6, so ::1 has to be written as an address rather
        than as the number 1. An IPv4 address is four decimal numbers from 0 to 255: a leading zero does not
        make a number octal, and shortened forms such as 10.1 are refused.

        An IPv4 address is also given as the IPv6 address that maps it (::ffff:192.168.1.1), and an
        IPv4-mapped IPv6 address as the IPv4 address it carries. The decimal, hexadecimal and binary numbers
        are of the address as given: 32 bits for IPv4, 128 for IPv6.
        """,
    Usage =
        "Use this instead of converting by hand: an address as a 32-bit or 128-bit number, in hexadecimal or binary, or mapped between IPv4 and IPv6, is easy to get wrong by a digit. For networks, masks and ranges use ip-subnet.",
    Aliases = [],
    Keywords = [Keyword.Ip, Keyword.Network, Keyword.Convert],
    Categories = [Category.Network, Category.Converter]
)]
public sealed class IpConverterTool : ToolBase<IpConverterTool.Args, IpConverterTool.Result>
{
    // ::ffff:0:0/96, the IPv6 block that holds every IPv4 address.
    private static readonly UInt128 MappedPrefix = (UInt128)0xFFFF << 32;

    protected override Result Execute(Args args)
    {
        if (string.IsNullOrWhiteSpace(args.Address))
        {
            throw new ToolException(ErrorCode.TextEmpty, nameof(Args.Address));
        }

        var address = Parse(args.Address.Trim())
                      ?? throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Address));

        var value = address.Value;
        var ipv6 = address.IsIpv4 ? MappedPrefix | value : value;
        var ipv4 = address.IsIpv4 || value >> 32 == 0xFFFF
            ? IpAddressText.Format(value & uint.MaxValue, IpAddressText.Ipv4Bits)
            : null;

        return new Result(
            Version: address.IsIpv4 ? 4 : 6,
            Ipv4: ipv4,
            Decimal: value.ToString(CultureInfo.InvariantCulture),
            Hexadecimal: "0x" + value.ToString(address.IsIpv4 ? "X8" : "X32", CultureInfo.InvariantCulture),
            Binary: "0b" + value.ToString(address.IsIpv4 ? "B32" : "B128", CultureInfo.InvariantCulture),
            Ipv6: IpAddressText.Format(ipv6, IpAddressText.Ipv6Bits),
            Ipv6Expanded: string.Join(':', Enumerable.Range(0, 8)
                .Select(group => ((ushort)(ipv6 >> (112 - 16 * group))).ToString("x4", CultureInfo.InvariantCulture)))
        );
    }

    private static IpAddressValue? Parse(string text)
    {
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return FromNumber(text[2..], NumberStyles.AllowHexSpecifier);
        }

        if (text.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            return FromNumber(text[2..], NumberStyles.AllowBinarySpecifier);
        }

        return text.All(char.IsAsciiDigit)
            ? FromNumber(text, NumberStyles.None)
            : IpAddressText.Parse(text);
    }

    private static IpAddressValue? FromNumber(string digits, NumberStyles style)
    {
        if (digits.Length == 0 || !UInt128.TryParse(digits, style, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        return new IpAddressValue(value, value <= uint.MaxValue ? IpAddressText.Ipv4Bits : IpAddressText.Ipv6Bits);
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("An IPv4 address (192.168.1.1), an IPv6 address (2001:db8::1), or the address as a decimal number (3232235777), a hexadecimal number after 0x (0xC0A80101) or a binary number after 0b")]
        string? Address
    );

    public sealed record Result(
        [property: Description("4 or 6, as the input was read")]
        int Version,
        [property: DisplayName("IPv4"),
                   Description("The IPv4 address in dotted decimal; for an IPv6 address, the IPv4 address an IPv4-mapped one carries, and none otherwise")]
        string? Ipv4,
        [property: Description("The address as one decimal number")]
        string Decimal,
        [property: Description("The address as a hexadecimal number after 0x: 8 digits for IPv4, 32 for IPv6")]
        string Hexadecimal,
        [property: Description("The address as a binary number after 0b: 32 digits for IPv4, 128 for IPv6")]
        string Binary,
        [property: DisplayName("IPv6"),
                   Description("The IPv6 address in its shortest form (RFC 5952); for an IPv4 address, the IPv4-mapped IPv6 address ::ffff:a.b.c.d")]
        string Ipv6,
        [property: DisplayName("IPv6 Expanded"),
                   Description("The same IPv6 address with all eight groups written out in four digits")]
        string Ipv6Expanded
    ) : ToolResult
    {
        public Result() : this(0, null, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
        {
        }
    }
}
