using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Dev.Tools.Networking;

/// <summary>An IP address as a number, with the width that tells the version: 32 bits for IPv4, 128 for IPv6.</summary>
internal readonly record struct IpAddressValue(UInt128 Value, int Width)
{
    public bool IsIpv4 => Width == IpAddressText.Ipv4Bits;
}

/// <summary>Reads and writes IP addresses in their usual text form: dotted IPv4 and RFC 5952 IPv6.</summary>
internal static class IpAddressText
{
    public const int Ipv4Bits = 32;
    public const int Ipv6Bits = 128;

    public static IpAddressValue? Parse(string text)
    {
        if (text.Contains(':'))
        {
            // A zone index (fe80::1%eth0) names an interface on one machine, not part of the address.
            if (text.Contains('%') || !IPAddress.TryParse(text, out var ip) || ip.AddressFamily != AddressFamily.InterNetworkV6)
            {
                return null;
            }

            return new IpAddressValue(BinaryPrimitives.ReadUInt128BigEndian(ip.GetAddressBytes()), Ipv6Bits);
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

        return new IpAddressValue(value, Ipv4Bits);
    }

    public static string Format(UInt128 value, int width)
    {
        if (width == Ipv4Bits)
        {
            var address = (uint)value;
            return string.Create(CultureInfo.InvariantCulture,
                $"{address >> 24}.{address >> 16 & 0xFF}.{address >> 8 & 0xFF}.{address & 0xFF}");
        }

        // IPv4-mapped addresses with the IPv4 part dotted, as RFC 5952 recommends; IPAddress does so for all but ::ffff:0.0.0.0.
        if (value >> Ipv4Bits == 0xFFFF)
        {
            return "::ffff:" + Format(value & uint.MaxValue, Ipv4Bits);
        }

        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteUInt128BigEndian(bytes, value);
        return new IPAddress(bytes).ToString();
    }
}
