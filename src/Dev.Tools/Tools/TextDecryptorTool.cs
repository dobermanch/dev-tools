using System.Security.Cryptography;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "text-decryptor",
    Title = "Text Decryptor",
    Description =
        """
        Decrypts text produced by the text-encryptor tool.

        The key, algorithm and encoding must match the ones used to encrypt; for block cipher modes
        the IV is read from the start of the input. A wrong key usually fails outright, but only
        AES-GCM authenticates the ciphertext - the other algorithms can return meaningless text.
        """,
    Usage =
        "Only reverses this suite's text-encryptor output - ciphertext from another program will usually fail because the IV placement and key derivation differ. A decryption failure means the key, algorithm or format did not match; it does not narrow down which.",
    Aliases = ["decrypt"],
    Keywords = [Keyword.Encrypt, Keyword.Decode, Keyword.Text, Keyword.String],
    Categories = [Category.Crypto, Category.Security]
)]
public sealed class TextDecryptorTool : ToolBase<TextDecryptorTool.Args, TextDecryptorTool.Result>
{
    protected override Result Execute(Args args)
    {
        // Both are checked before either is reported, so a caller missing both hears about both.
        var errors = new List<ToolError>();
        if (string.IsNullOrEmpty(args.EncryptedText))
        {
            errors.Add(new ToolError(ErrorCode.TextEmpty, nameof(Args.EncryptedText)));
        }

        if (string.IsNullOrEmpty(args.Key))
        {
            errors.Add(new ToolError(ErrorCode.InputNotValid, nameof(Args.Key)));
        }

        if (errors.Count > 0)
        {
            throw new ToolException(errors);
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
                _ => throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Algorithm))
            };

            return new Result(Encoding.UTF8.GetString(plainBytes));
        }
        catch (ToolException)
        {
            throw;
        }
        catch (FormatException)
        {
            throw new ToolException(ErrorCode.WrongFormat, nameof(Args.EncryptedText));
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
        [Description("AES-128 in CBC mode with PKCS#7 padding")]
        Aes128Cbc,

        [Description("AES-256 in CBC mode with PKCS#7 padding")]
        Aes256Cbc,

        [Description("AES-256 in GCM mode, authenticated")]
        Aes256Gcm,

        [Description("TripleDES in CBC mode; legacy, 64-bit block")]
        TripleDes,

        [Description("RC4 stream cipher; broken, for legacy interoperability only")]
        Rc4
    }

    public enum InputFormat
    {
        [Description("Base64")]
        Base64,

        [Description("Hexadecimal")]
        Hex
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Base64 or hex encoded ciphertext to decrypt")]
        string EncryptedText,
        [property: DisplayName("Secret Key"),
                   Description("Secret key; must be the one used to encrypt")]
        string Key,
        [property: Description("Algorithm the text was encrypted with")]
        EncryptionAlgorithm Algorithm = EncryptionAlgorithm.Aes256Cbc,
        [property: Description("Encoding of the ciphertext; must match the format used when encrypting")]
        InputFormat InputFormat = InputFormat.Base64
    );

    public sealed record Result(
        [property: PipeOutput,
                   Description("The recovered plain text")]
        string DecryptedText
    ) : ToolResult
    {
        public Result() : this(string.Empty)
        {
        }
    }
}