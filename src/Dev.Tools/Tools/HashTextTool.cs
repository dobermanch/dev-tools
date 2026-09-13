using System.Buffers;
using System.Security.Cryptography;
using Dev.Tools.Cryptography;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "hash",
    Title = "Hash Generator",
    Description =
        """
        Generates a cryptographic hash of text or a file using MD5, SHA-1, SHA-256, SHA-384 or SHA-512,
        returned as lowercase hexadecimal. Text is hashed as UTF-8 bytes, a file as its raw bytes.
        """,
    Usage =
        "Hashing only - there is no way back from a digest to the text. MD5 and SHA-1 are broken for security purposes; use them only for checksums and compatibility, and prefer SHA-256 or stronger otherwise.",
    Aliases = [],
    Keywords = [Keyword.Generate, Keyword.Text, Keyword.String, Keyword.Hash],
    Categories = [Category.Text, Category.Crypto, Category.Security]
)]
public sealed class HashTextTool(IMd5Hash md5Hash) : ToolBase<HashTextTool.Args, HashTextTool.Result>
{
    private const int BufferSize = 81_920;

    protected override async Task<Result> ExecuteAsync(Args args, CancellationToken cancellationToken)
    {
        // Overriding ExecuteAsync skips the check ToolBase makes before running a tool.
        cancellationToken.ThrowIfCancellationRequested();

        if (args.Text is null)
        {
            throw new ToolException(ErrorCode.TextEmpty, nameof(Args.Text));
        }

        byte[]? hash;
        try
        {
            // The algorithm is settled before any content is read, so a bad one does not consume stdin first.
            hash = args.Algorithm switch
            {
                HashAlgorithm.Md5 => await ComputeMd5Async(args.Text, cancellationToken),
                HashAlgorithm.Sha1 => await ComputeAsync(args.Text, HashAlgorithmName.SHA1, cancellationToken),
                HashAlgorithm.Sha256 => await ComputeAsync(args.Text, HashAlgorithmName.SHA256, cancellationToken),
                HashAlgorithm.Sha384 => await ComputeAsync(args.Text, HashAlgorithmName.SHA384, cancellationToken),
                HashAlgorithm.Sha512 => await ComputeAsync(args.Text, HashAlgorithmName.SHA512, cancellationToken),
                _ => throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Algorithm))
            };
        }
        catch (CryptographicException)
        {
            // No input makes hashing fail, so this is not the caller's doing - and it was never a
            // decryption failure, which is what it used to report.
            throw new ToolException(ErrorCode.Unknown);
        }

        // Empty text and an empty file alike: nobody asks for the hash of nothing on purpose.
        if (hash is null)
        {
            throw new ToolException(ErrorCode.TextEmpty, nameof(Args.Text));
        }

        return new Result(Convert.ToHexStringLower(hash));
    }

    // IMd5Hash hashes a whole buffer - the WebAssembly host substitutes a managed implementation that
    // works that way - so MD5 reads the content into memory.
    private async Task<byte[]?> ComputeMd5Async(ToolContent content, CancellationToken cancellationToken)
    {
        var bytes = await content.ReadAllBytesAsync(cancellationToken);
        return bytes.Length == 0 ? null : md5Hash.ComputeHash(bytes);
    }

    // The SHA family hashes as it reads, so a file of any size is never held in memory.
    private static async Task<byte[]?> ComputeAsync(
        ToolContent content,
        HashAlgorithmName algorithm,
        CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(algorithm);
        await using var stream = content.OpenRead();

        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            var total = 0L;
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
            {
                hash.AppendData(buffer, 0, read);
                total += read;
            }

            return total == 0 ? null : hash.GetHashAndReset();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public enum HashAlgorithm
    {
        [Description("MD5 - 128-bit, fast, not collision resistant")]
        Md5,

        [Description("SHA-1 - 160-bit, deprecated for security use")]
        Sha1,

        [Description("SHA-256 - 256-bit, the general-purpose default")]
        Sha256,

        [Description("SHA-384 - 384-bit")]
        Sha384,

        [Description("SHA-512 - 512-bit")]
        Sha512
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Text or file content to hash; text is read as UTF-8")]
        ToolContent Text,
        [property: Description("Hashing algorithm to use")]
        HashAlgorithm Algorithm = HashAlgorithm.Md5
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("Hash Value"),
                   Description("The digest as lowercase hexadecimal")]
        string Data
    ) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}
