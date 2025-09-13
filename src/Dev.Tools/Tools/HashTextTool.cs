using System.Security.Cryptography;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "hash",
    Aliases = [],
    Keywords = [Keyword.Generate, Keyword.Text, Keyword.String, Keyword.Hash],
    Categories = [Category.Text, Category.Crypto, Category.Security]
)]
public sealed class HashTextTool : ToolBase<HashTextTool.Args, HashTextTool.Result>
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
    {
#if WEBASSEMBLY
        return Dev.Tools.Core.Cryptography.MD5.ComputeHash(data);
#else
        return MD5.HashData(data);
#endif
    }

    private byte[] ComputeSha1(byte[] data)
    {
        return SHA1.HashData(data);
    }

    private byte[] ComputeSha256(byte[] data)
    {
        return SHA256.HashData(data);
    }

    private byte[] ComputeSha384(byte[] data)
    {
        return SHA384.HashData(data);
    }

    private byte[] ComputeSha512(byte[] data)
    {
        return SHA512.HashData(data);
    }

    public enum HashAlgorithm
    {
        Md5,
        Sha1,
        Sha256,
        Sha384,
        Sha512
    }

    public record Args
    {
        public string? Text { get; set; }
        public HashAlgorithm Algorithm { get; set; }
    }

    public record Result(string Data) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}
