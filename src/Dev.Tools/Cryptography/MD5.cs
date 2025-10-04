using System.Security.Cryptography;

namespace Dev.Tools.Cryptography;

public interface IMd5Hash
{
    byte[] ComputeHash(byte[] input);
}

public sealed class Md5Hash : IMd5Hash
{
    public byte[] ComputeHash(byte[] input) => MD5.HashData(input);
}