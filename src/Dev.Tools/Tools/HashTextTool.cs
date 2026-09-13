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

        Given a Key, it computes an HMAC (RFC 2104) instead: a hash keyed with a shared secret, which only
        someone holding the same key can reproduce - the way webhook signatures and signed API requests
        are checked. The key is read as UTF-8, and an empty key is the same as none.
        """,
    Usage =
        "Hashing only - there is no way back from a digest to the text. MD5 and SHA-1 are broken for security purposes; use them only for checksums and compatibility, and prefer SHA-256 or stronger otherwise. To check a webhook or request signature, pass the shared secret as Key and compare the result with the signature the other side sent.",
    Aliases = [],
    Keywords = [Keyword.Generate, Keyword.Text, Keyword.String, Keyword.Hash, Keyword.Hmac],
    Categories = [Category.Text, Category.Crypto, Category.Security]
)]
public sealed class HashTextTool(IMd5Hash md5Hash) : ToolBase<HashTextTool.Args, HashTextTool.Result>
{
    private const int BufferSize = 81_920;

    // The block size of MD5, which RFC 2104 pads the key to.
    private const int Md5BlockSize = 64;

    protected override async Task<Result> ExecuteAsync(Args args, CancellationToken cancellationToken)
    {
        // Overriding ExecuteAsync skips the check ToolBase makes before running a tool.
        cancellationToken.ThrowIfCancellationRequested();

        if (args.Text is null)
        {
            throw new ToolException(ErrorCode.TextEmpty, nameof(Args.Text));
        }

        var key = string.IsNullOrEmpty(args.Key) ? null : Encoding.UTF8.GetBytes(args.Key);

        byte[]? hash;
        try
        {
            // The algorithm is settled before any content is read, so a bad one does not consume stdin first.
            hash = args.Algorithm switch
            {
                HashAlgorithm.Md5 => await ComputeMd5Async(args.Text, key, cancellationToken),
                HashAlgorithm.Sha1 => await ComputeAsync(args.Text, HashAlgorithmName.SHA1, key, cancellationToken),
                HashAlgorithm.Sha256 => await ComputeAsync(args.Text, HashAlgorithmName.SHA256, key, cancellationToken),
                HashAlgorithm.Sha384 => await ComputeAsync(args.Text, HashAlgorithmName.SHA384, key, cancellationToken),
                HashAlgorithm.Sha512 => await ComputeAsync(args.Text, HashAlgorithmName.SHA512, key, cancellationToken),
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
    private async Task<byte[]?> ComputeMd5Async(ToolContent content, byte[]? key, CancellationToken cancellationToken)
    {
        var bytes = await content.ReadAllBytesAsync(cancellationToken);
        if (bytes.Length == 0)
        {
            return null;
        }

        return key is null ? md5Hash.ComputeHash(bytes) : ComputeHmacMd5(key, bytes);
    }

    /// <summary>
    /// HMAC (RFC 2104) written out over <see cref="IMd5Hash"/>: the browser has no MD5, so it has no
    /// HMACMD5 either. MD5(key XOR opad || MD5(key XOR ipad || message)), the key first hashed when it
    /// is longer than a block and zero-padded to one.
    /// </summary>
    private byte[] ComputeHmacMd5(byte[] key, byte[] message)
    {
        if (key.Length > Md5BlockSize)
        {
            key = md5Hash.ComputeHash(key);
        }

        var inner = new byte[Md5BlockSize + message.Length];
        var outer = new byte[Md5BlockSize];
        for (var i = 0; i < Md5BlockSize; i++)
        {
            var keyByte = i < key.Length ? key[i] : (byte)0;
            inner[i] = (byte)(keyByte ^ 0x36);
            outer[i] = (byte)(keyByte ^ 0x5C);
        }

        message.CopyTo(inner, Md5BlockSize);

        return md5Hash.ComputeHash([.. outer, .. md5Hash.ComputeHash(inner)]);
    }

    // The SHA family hashes as it reads, so a file of any size is never held in memory.
    private static async Task<byte[]?> ComputeAsync(
        ToolContent content,
        HashAlgorithmName algorithm,
        byte[]? key,
        CancellationToken cancellationToken)
    {
        using var hash = key is null
            ? IncrementalHash.CreateHash(algorithm)
            : IncrementalHash.CreateHMAC(algorithm, key);
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
        HashAlgorithm Algorithm = HashAlgorithm.Md5,
        [property: Description("Secret key, read as UTF-8, that makes the result an HMAC; without one, or with an empty one, the plain hash is returned")]
        string? Key = null
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("Hash Value"),
                   Description("The digest, or the HMAC when a key is given, as lowercase hexadecimal")]
        string Data
    ) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}
