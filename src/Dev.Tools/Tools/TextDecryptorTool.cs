using System.Security.Cryptography;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "text-decryptor",
    Aliases = ["decrypt"],
    Keywords = [Keyword.Encrypt, Keyword.Decode, Keyword.Text, Keyword.String],
    Categories = [Category.Crypto, Category.Security]
)]
public sealed class TextDecryptorTool : ToolBase<TextDecryptorTool.Args, TextDecryptorTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.EncryptedText))
        {
            throw new ToolException(ErrorCode.TextEmpty);
        }

        if (string.IsNullOrEmpty(args.Key))
        {
            throw new ToolException(ErrorCode.InputNotValid);
        }

        try
        {
            var encryptedBytes = args.InputFormat switch
            {
                InputFormat.Hex => Convert.FromHexString(args.EncryptedText),
                _ => Convert.FromBase64String(args.EncryptedText)
            };

            var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(args.Key));
            var plainBytes = args.Algorithm switch
            {
                EncryptionAlgorithm.Aes128Cbc => DecryptAesCbc(encryptedBytes, keyBytes[..16]),
                EncryptionAlgorithm.Aes256Cbc => DecryptAesCbc(encryptedBytes, keyBytes),
                EncryptionAlgorithm.Aes256Gcm => DecryptAesGcm(encryptedBytes, keyBytes),
                EncryptionAlgorithm.TripleDes => DecryptTripleDes(encryptedBytes, keyBytes[..24]),
                EncryptionAlgorithm.Rc4 => DecryptRc4(encryptedBytes, keyBytes),
                _ => throw new ToolException(ErrorCode.InputNotValid)
            };

            return new Result(Encoding.UTF8.GetString(plainBytes));
        }
        catch (ToolException)
        {
            throw;
        }
        catch (FormatException)
        {
            throw new ToolException(ErrorCode.WrongFormat);
        }
        catch
        {
            throw new ToolException(ErrorCode.FailedToDecrypt);
        }
    }

    // AES-CBC: input = IV[16] + Ciphertext
    private static byte[] DecryptAesCbc(byte[] data, byte[] key)
    {
        var iv = data[..16];
        var ciphertext = data[16..];

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    // AES-GCM: input = Nonce[12] + Tag[16] + Ciphertext
    private static byte[] DecryptAesGcm(byte[] data, byte[] key)
    {
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        var nonce = data[..nonceSize];
        var tag = data[nonceSize..(nonceSize + tagSize)];
        var ciphertext = data[(nonceSize + tagSize)..];

        var plaintext = new byte[ciphertext.Length];
        using var aesGcm = new AesGcm(key, tagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    // TripleDES-CBC: input = IV[8] + Ciphertext
    private static byte[] DecryptTripleDes(byte[] data, byte[] key)
    {
        var iv = data[..8];
        var ciphertext = data[8..];

        using var des = TripleDES.Create();
        des.Key = key;
        des.IV = iv;

        using var decryptor = des.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    // RC4 is symmetric — same operation as encryption
    private static byte[] DecryptRc4(byte[] data, byte[] key)
    {
        var s = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            s[i] = (byte)i;
        }

        var j = 0;
        for (var i = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) & 0xFF;
            (s[i], s[j]) = (s[j], s[i]);
        }

        var result = new byte[data.Length];
        var x = 0;
        var y = 0;
        for (var i = 0; i < data.Length; i++)
        {
            x = (x + 1) & 0xFF;
            y = (y + s[x]) & 0xFF;
            (s[x], s[y]) = (s[y], s[x]);
            result[i] = (byte)(data[i] ^ s[(s[x] + s[y]) & 0xFF]);
        }

        return result;
    }

    public enum EncryptionAlgorithm
    {
        Aes128Cbc,
        Aes256Cbc,
        Aes256Gcm,
        TripleDes,
        Rc4
    }

    public enum InputFormat
    {
        Base64,
        Hex
    }

    public sealed record Args(
        [property: PipeInput] string EncryptedText,
        string Key,
        EncryptionAlgorithm Algorithm = EncryptionAlgorithm.Aes256Cbc,
        InputFormat InputFormat = InputFormat.Base64
    );

    public sealed record Result([property: PipeOutput] string DecryptedText) : ToolResult
    {
        public Result() : this(string.Empty)
        {
        }
    }
}