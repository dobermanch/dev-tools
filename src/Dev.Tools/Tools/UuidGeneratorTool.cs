using Dev.Tools.Core;
using System.Security.Cryptography;
using System.Text;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "uuid-gen",
    Aliases = ["ug"]
// Keywords = [ToolKeyword.Uuid, ToolKeyword.Guid, ToolKeyword.Generate]
// Categories = [ToolCategory.Converter],
// ErrorCodes = [ToolError.Unknown, "NAMESAPCE_EMPTY"]
)]
public sealed class UuidGeneratorTool : ToolBase<UuidGeneratorTool.Args, UuidGeneratorTool.Result>
{
    private readonly static Guid _max = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");

    protected override Result Execute(Args args)
    {
        Guid[] guids;

        switch (args.Type)
        {
            case UuidType.Nil:
                guids = Generate(args, _ => Guid.Empty);
                break;
            case UuidType.Max:
                guids = Generate(args, _ => _max);
                break;
            case UuidType.V3:
                if (args.Namespace is null)
                {
                    return Failed("NAMESPACE_EMPTY");
                }

                guids = Generate(args, _ => NewUuidV3(args.Namespace.Value, args.Name ?? string.Empty));
                break;
            case UuidType.V4:
                guids = Generate(args, _ => Guid.NewGuid());
                break;
            case UuidType.V5:
                if (args.Namespace is null)
                {
                    return Failed("NAMESPACE_EMPTY");
                }

                guids = Generate(args, _ => NewUuidV5(args.Namespace.Value, args.Name ?? string.Empty));
                break;
            case UuidType.V7:
                long timestamp = (args.time ?? DateTimeOffset.UtcNow).ToUnixTimeMilliseconds();
                guids = Generate(args, _ => NewUuidV7(timestamp));
                break;
            default:
                guids = [];
                break;
        }

        return new(guids);
    }

    private static Guid[] Generate(Args args, Func<long, Guid> getGuid)
    {
        var guilds = new Guid[args.Count];
        Parallel.For(0, guilds.Length <= 0 ? 1 : guilds.Length, index => guilds[index] = getGuid(index));

        return guilds;
    }

    private static Guid NewUuidV3(Guid namespaceUuid, string name)
    {
        using var md5 = MD5.Create();
        return NewHashBasdUuid(md5, namespaceUuid, name);
    }

    private static Guid NewUuidV5(Guid namespaceUuid, string name)
    {
        using var sha1 = SHA1.Create();
        return NewHashBasdUuid(sha1, namespaceUuid, name);
    }

    private static Guid NewHashBasdUuid(HashAlgorithm algorithm, Guid namespaceUuid, string name)
    {
        byte[] namespaceBytes = namespaceUuid.ToByteArray();
        byte[] nameBytes = Encoding.UTF8.GetBytes(name);
        byte[] bytes = namespaceBytes.Concat(nameBytes).ToArray();

        byte[] hash = algorithm.ComputeHash(bytes);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50); // Set version to 5 (0b0101)
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // Set variant to RFC 4122

        return new Guid(hash[0..16]);
    }

    private static Guid NewUuidV7(long timeStampInMs)
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
        Span<byte> random = bytes.AsSpan().Slice(6);
        RandomNumberGenerator.Fill(random);

        // add mask to set guid version
        // bytes [6]: 4 bits dedicated to guid version (version: 7)
        // bytes [6]: 4 bits dedicated to random part
        bytes[6] &= 0x0F;
        bytes[6] += 0x70;

        return new Guid(bytes, true);
    }

    public enum UuidType : byte
    {
        Nil,
        V3,
        V4,
        V5,
        V7,
        Max
    }

    // TODO: Taking into account that each UUID type has it own set of parameters,
    // it make sence to make separate tool for each type
    public record Args(
        UuidType Type, 
        int Count = 1, 
        Guid? Namespace = null, 
        string? Name = null,
        DateTimeOffset? time = null
    ) : ToolArgs;

    public record Result(IReadOnlyCollection<Guid> Guilds) : ToolResult
    {
        public Result() : this([]) { }
    }
}
