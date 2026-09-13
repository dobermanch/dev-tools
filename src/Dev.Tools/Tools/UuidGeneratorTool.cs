using System.Security.Cryptography;
using Dev.Tools.Cryptography;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "uuid-generator",
    Title = "UUID Generator",
    Description =
        """
        Generates UUIDs (GUIDs) in the versions defined by RFC 9562.

        Version 4 is random and is the default. Versions 3 and 5 are derived from a namespace and a
        name, so the same pair always yields the same UUID. Version 7 embeds a millisecond timestamp
        and therefore sorts by creation time. Formatting - case, hyphens and wrapping brackets - is
        applied to every generated value.
        """,
    Usage =
        "Namespace and Name are required for V3 and V5 and ignored otherwise; Time applies to V7 only. Prefer V4 for identifiers that must not be guessable and V7 where database insert order matters. V3 uses MD5 and exists for compatibility - use V5 for new name-based identifiers.",
    Aliases = ["uuid"],
    Keywords = [Keyword.Uuid, Keyword.Guid, Keyword.Generate, Keyword.Text, Keyword.String],
    Categories = [Category.Crypto]
)]
public sealed class UuidGeneratorTool(IMd5Hash md5Hash, IRandomSource? random = null, TimeProvider? time = null)
    : ToolBase<UuidGeneratorTool.Args, UuidGeneratorTool.Result>
{
    private readonly IRandomSource _random = random ?? CryptographicRandomSource.Instance;
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    private static readonly Guid Max = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");

    protected override Result Execute(Args args, CancellationToken cancellationToken)
    {
        Guid[] guids;

        switch (args.Type)
        {
            case UuidType.Nil:
                guids = Generate(args, cancellationToken, _ => Guid.Empty);
                break;
            case UuidType.Max:
                guids = Generate(args, cancellationToken, _ => Max);
                break;
            case UuidType.V3:
                if (args.Namespace is null)
                {
                    throw new ToolException(ErrorCode.NamespaceEmpty);
                }

                guids = Generate(args, cancellationToken, _ => NewUuidV3(args.Namespace.Value, args.Name ?? string.Empty));
                break;
            case UuidType.V4:
                guids = Generate(args, cancellationToken, _ => NewUuidV4());
                break;
            case UuidType.V5:
                if (args.Namespace is null)
                {
                    throw new ToolException(ErrorCode.NamespaceEmpty);
                }

                guids = Generate(args, cancellationToken, _ => NewUuidV5(args.Namespace.Value, args.Name ?? string.Empty));
                break;
            case UuidType.V7:
                long timestamp = (args.Time ?? _time.GetUtcNow()).ToUnixTimeMilliseconds();
                guids = Generate(args, cancellationToken, _ => NewUuidV7(timestamp));
                break;
            default:
                guids = [];
                break;
        }

        string[] formattedGuids = guids.Select(g => FormatGuid(g, args)).ToArray();

        return new(formattedGuids);
    }

    private static Guid[] Generate(Args args, CancellationToken cancellationToken, Func<long, Guid> getGuid)
    {
        var guilds = new Guid[args.Count <= 0 ? 1 : args.Count];
        Parallel.For(
            0,
            guilds.Length,
            new ParallelOptions { CancellationToken = cancellationToken },
            index => guilds[index] = getGuid(index));

        return guilds;
    }

    /// <summary>
    /// 122 random bits with the version and variant stamped in. Built from the injected source rather
    /// than <c>Guid.NewGuid()</c>, which reads its own randomness and so could never be asserted on.
    /// </summary>
    private Guid NewUuidV4()
    {
        Span<byte> bytes = stackalloc byte[16];
        _random.Fill(bytes);

        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40); // version 4
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // RFC 9562 variant

        return new Guid(bytes, bigEndian: true);
    }

    private Guid NewUuidV3(Guid namespaceUuid, string name)
    {
        return NewHashBasedUuid(md5Hash.ComputeHash, namespaceUuid, name, version: 3);
    }

    private static Guid NewUuidV5(Guid namespaceUuid, string name)
    {
        return NewHashBasedUuid(SHA1.HashData, namespaceUuid, name, version: 5);
    }

    private static Guid NewHashBasedUuid(Func<byte[], byte[]> computeHash, Guid namespaceUuid, string name, byte version)
    {
        // The namespace is hashed in RFC 4122 network byte order, not in Guid's
        // little-endian layout, otherwise the result differs from every other implementation.
        byte[] namespaceBytes = namespaceUuid.ToByteArray(bigEndian: true);
        byte[] nameBytes = Encoding.UTF8.GetBytes(name);
        byte[] bytes = [.. namespaceBytes, .. nameBytes];

        byte[] hash = computeHash(bytes);
        hash[6] = (byte)((hash[6] & 0x0F) | (version << 4)); // Set version (3 for MD5, 5 for SHA-1)
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // Set variant to RFC 4122

        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }

    private Guid NewUuidV7(long timeStampInMs)
    {
        var bytes = new byte[16];

        // Fill time part
        // bytes [0-5]: datetimeoffset yyyy-MM-dd hh:mm:ss fff
        byte[] current = BitConverter.GetBytes(timeStampInMs);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(current);
        }

        current[2..8].CopyTo(bytes, 0);

        // Fill random part
        // bytes [7-15]: random part
        Span<byte> random = bytes.AsSpan()[6..];
        _random.Fill(random);

        // add mask to set guid version
        // bytes [6]: 4 bits dedicated to guid version (version: 7)
        // bytes [6]: 4 bits dedicated to random part
        bytes[6] &= 0x0F;
        bytes[6] += 0x70;

        // bytes [8]: the top two bits are the RFC 9562 variant (10). These used to be left random,
        // so three V7 values in four carried the wrong variant.
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes, true);
    }

    private static string FormatGuid(Guid guid, Args args)
    {
        string format = args.Hyphens ? "D" : "N";
        var uuidString = guid.ToString(format);

        if (args.Case == UuidCase.Uppercase)
        {
            uuidString = uuidString.ToUpperInvariant();
        }

        return args.Brackets switch
        {
            UuidBrackets.Braces => $"{{{uuidString}}}",
            UuidBrackets.Parentheses => $"({uuidString})",
            UuidBrackets.SquareBrackets => $"[{uuidString}]",
            _ => uuidString
        };
    }

    public enum UuidType : byte
    {
        [Description("Nil UUID, all zeros")]
        Nil,

        [Description("Version 3, MD5 hash of a namespace and a name")]
        V3,

        [Description("Version 4, random")]
        V4,

        [Description("Version 5, SHA-1 hash of a namespace and a name")]
        V5,

        [Description("Version 7, timestamp-based and sortable")]
        V7,

        [Description("Max UUID, all ones")]
        Max
    }

    public enum UuidCase
    {
        [Description("Lowercase, the canonical form")]
        Lowercase,

        [Description("Uppercase")]
        Uppercase
    }

    public enum UuidBrackets
    {
        [Description("No wrapping characters")]
        None,

        [Description("Wrapped in braces: {...}")]
        Braces,

        [Description("Wrapped in parentheses: (...)")]
        Parentheses,

        [Description("Wrapped in square brackets: [...]")]
        SquareBrackets
    }

    // TODO: Taking into account that each UUID type has it own set of parameters,
    // it make sense to make separate tool for each type
    public sealed record Args(
        [property: DisplayName("UUID Version"),
                   Description("UUID version to generate")]
        UuidType Type = UuidType.V4,
        [property: Description("Number of UUIDs to generate; values below 1 are treated as 1")]
        int Count = 1,
        [property: Description("Namespace UUID; required for V3 and V5, ignored otherwise")]
        Guid? Namespace = null,
        [property: Description("Name hashed together with the namespace; required for V3 and V5, ignored otherwise")]
        string? Name = null,
        [property: DisplayName("Timestamp"),
                   Description("Timestamp to embed in a V7 UUID; defaults to the current time, ignored for other versions")]
        DateTime? Time = null,
        [property: Description("Separate the five groups with hyphens")]
        bool Hyphens = true,
        [property: Description("Letter case of the hexadecimal digits")]
        UuidCase Case = UuidCase.Lowercase,
        [property: Description("Characters wrapped around each UUID")]
        UuidBrackets Brackets = UuidBrackets.None
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("UUIDs"),
                   Description("The generated UUIDs, one per requested count")]
        IReadOnlyCollection<string> Data
    ) : ToolResult
    {
        public Result() : this([]) { }
    }
}