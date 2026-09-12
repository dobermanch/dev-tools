using System.Security.Cryptography;
using Dev.Tools.Cryptography;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "hash",
    Title = "Hash Generator",
    Description =
        "Generates a cryptographic hash of text using MD5, SHA-1, SHA-256, SHA-384 or SHA-512. The input is hashed as UTF-8 bytes and the digest is returned as lowercase hexadecimal.",
    Usage =
        "Hashing only - there is no way back from a digest to the text. MD5 and SHA-1 are broken for security purposes; use them only for checksums and compatibility, and prefer SHA-256 or stronger otherwise.",
    Aliases = [],
    Keywords = [Keyword.Generate, Keyword.Text, Keyword.String, Keyword.Hash],
    Categories = [Category.Text, Category.Crypto, Category.Security]
)]
public sealed class HashTextTool(IMd5Hash md5Hash) : ToolBase<HashTextTool.Args, HashTextTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            throw new ToolException(ErrorCode.TextEmpty);
        }

        byte[] hash;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(args.Text);
            hash = args.Algorithm switch
            {
                HashAlgorithm.Md5 => ComputeMd5(bytes),
                HashAlgorithm.Sha1 => ComputeSha1(bytes),
                HashAlgorithm.Sha256 => ComputeSha256(bytes),
                HashAlgorithm.Sha384 => ComputeSha384(bytes),
                HashAlgorithm.Sha512 => ComputeSha512(bytes),
                _ => throw new ArgumentOutOfRangeException()
            };

        }
        catch (CryptographicException)
        {
            throw new ToolException(ErrorCode.FailedToDecrypt);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ToolException(ErrorCode.InputNotValid);    
        }   
        
        var result = new StringBuilder();
        foreach (var ch in hash)
        {
            result.Append(ch.ToString("x2"));
        }

        return new Result(result.ToString());
    }

    private byte[] ComputeMd5(byte[] data) 
        => md5Hash.ComputeHash(data);

    private byte[] ComputeSha1(byte[] data) 
        => SHA1.HashData(data);

    private byte[] ComputeSha256(byte[] data) 
        => SHA256.HashData(data);

    private byte[] ComputeSha384(byte[] data)
        => SHA384.HashData(data);

    private byte[] ComputeSha512(byte[] data) 
        => SHA512.HashData(data);

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
                   Description("Text to hash, read as UTF-8")]
        string Text,
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
