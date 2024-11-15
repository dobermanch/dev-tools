﻿using System.Security.Cryptography;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "hash",
    Aliases = [],
    Keywords = [Keyword.Generate, Keyword.Text, Keyword.String, Keyword.Hash],
    Categories = [Category.Text, Category.Crypto, Category.Security],
    ErrorCodes = [ErrorCode.Unknown, ErrorCode.TextEmpty]
)]
public sealed class HashTextTool : ToolBase<HashTextTool.Args, HashTextTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            return Failed(ErrorCode.TextEmpty);
        }

        var bytes = Encoding.UTF8.GetBytes(args.Text);

        byte[] data = args.Algorithm switch
        {
            HashAlgorithm.Md5 => ComputeMd5(bytes),
            HashAlgorithm.Sha1 => ComputeSha1(bytes),
            HashAlgorithm.Sha256 => ComputeSha256(bytes),
            HashAlgorithm.Sha384 => ComputeSha384(bytes),
            HashAlgorithm.Sha512 => ComputeSha512(bytes),
            _ => throw new ArgumentOutOfRangeException()
        };

        var result = new StringBuilder();
        foreach (var ch in data)
        {
            result.Append(ch.ToString("x2"));
        }

        return new Result(result.ToString());
    }

    private byte[] ComputeMd5(byte[] data)
    {
        using var hash = MD5.Create();
        return hash.ComputeHash(data);
    }

    private byte[] ComputeSha1(byte[] data)
    {
        using var hash = SHA1.Create();
        return hash.ComputeHash(data);
    }

    private byte[] ComputeSha256(byte[] data)
    {
        using var hash = SHA256 .Create();
        return hash.ComputeHash(data);
    }

    private byte[] ComputeSha384(byte[] data)
    {
        using var hash = SHA384.Create();
        return hash.ComputeHash(data);
    }

    private byte[] ComputeSha512(byte[] data)
    {
        using var hash = SHA512.Create();
        return hash.ComputeHash(data);
    }

    public enum HashAlgorithm
    {
        Md5,
        Sha1,
        Sha256,
        Sha384,
        Sha512
    }

    public record Args(
        string Text,
        HashAlgorithm Algorithm
    ) : ToolArgs;

    public record Result(string Data) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}
