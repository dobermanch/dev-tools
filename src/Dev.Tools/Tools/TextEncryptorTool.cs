using System.Security.Cryptography;
using Dev.Tools.Cryptography;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "text-encryptor",
    Title = "Text Encryptor",
    Description =
        """
        Encrypts text with a symmetric algorithm and returns the ciphertext as Base64 or hex.

        The key is turned into algorithm-sized key material with a single SHA-256 pass, so the
        strength of the result is the strength of the key itself. For block cipher modes the
        randomly generated IV is prepended to the output, which is what text-decryptor expects.
        """,
    Usage =
        "This is a developer utility, not a key-management system: there is no password stretching, no salt and no integrity check outside GCM, so it is not suitable for protecting data at rest. Decrypt with text-decryptor using the same key, algorithm and format.",
    Aliases = ["encrypt"],
    Keywords = [Keyword.Encrypt, Keyword.Text, Keyword.String],
    Categories = [Category.Crypto, Category.Security]
)]
public sealed class TextEncryptorTool(IRandomSource? random = null)
    : ToolBase<TextEncryptorTool.Args, TextEncryptorTool.Result>
{
    // The IVs and the nonce must be unpredictable, which the default source guarantees. A test may
    // substitute a fixed one to get reproducible ciphertext; nothing else should.
    private readonly IRandomSource _random = random ?? CryptographicRandomSource.Instance;

    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            throw new ToolException(ErrorCode.TextEmpty);
        }

        if (string.IsNullOrEmpty(args.Key))
        {
            throw new ToolException(ErrorCode.InputNotValid);
        }

        try
        {
            var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(args.Key));
            var plainBytes = Encoding.UTF8.GetBytes(args.Text);

            var encrypted = args.Algorithm switch
            {
                EncryptionAlgorithm.Aes128Cbc => EncryptAesCbc(plainBytes, keyBytes[..16]),
                EncryptionAlgorithm.Aes256Cbc => EncryptAesCbc(plainBytes, keyBytes),
                EncryptionAlgorithm.Aes256Gcm => EncryptAesGcm(plainBytes, keyBytes),
                EncryptionAlgorithm.TripleDes => EncryptTripleDes(plainBytes, keyBytes[..24]),
                EncryptionAlgorithm.Rc4 => EncryptRc4(plainBytes, keyBytes),
                _ => throw new ToolException(ErrorCode.InputNotValid)
            };

            var output = args.OutputFormat switch
            {
                OutputFormat.Hex => Convert.ToHexString(encrypted).ToLowerInvariant(),
                _                => Convert.ToBase64String(encrypted)
            };

            return new Result(output);
        }
        catch (ToolException)
        {
            throw;
        }
        catch
        {
            throw new ToolException(ErrorCode.FailedToDecrypt);
        }
    }

    // AES-CBC: output = IV[16] + Ciphertext
    private byte[] EncryptAesCbc(byte[] data, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = RandomBytes(aes.BlockSize / 8);

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(data, 0, data.Length);

        var result = new byte[aes.IV.Length + ciphertext.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(ciphertext, 0, result, aes.IV.Length, ciphertext.Length);
        return result;
    }

    // AES-GCM: output = Nonce[12] + Tag[16] + Ciphertext
    private byte[] EncryptAesGcm(byte[] data, byte[] key)
    {
        var nonce = RandomBytes(AesGcm.NonceByteSizes.MaxSize);
        var ciphertext = new byte[data.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using var aesGcm = new AesGcm(key, tag.Length);
        aesGcm.Encrypt(nonce, data, ciphertext, tag);

        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);
        return result;
    }

    // TripleDES-CBC: output = IV[8] + Ciphertext
    private byte[] EncryptTripleDes(byte[] data, byte[] key)
    {
        using var des = TripleDES.Create();
        des.Key = key;
        des.IV = RandomBytes(des.BlockSize / 8);

        using var encryptor = des.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(data, 0, data.Length);

        var result = new byte[des.IV.Length + ciphertext.Length];
        Buffer.BlockCopy(des.IV, 0, result, 0, des.IV.Length);
        Buffer.BlockCopy(ciphertext, 0, result, des.IV.Length, ciphertext.Length);
        return result;
    }

    private byte[] RandomBytes(int count)
    {
        var bytes = new byte[count];
        _random.Fill(bytes);
        return bytes;
    }

    // RC4: output = Ciphertext (stream cipher, no IV)
    private static byte[] EncryptRc4(byte[] data, byte[] key)
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

    public enum OutputFormat
    {
        [Description("Base64")]
        Base64,

        [Description("Lowercase hexadecimal")]
        Hex
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Plain text to encrypt")]
        string Text,
        [property: DisplayName("Secret Key"),
                   Description("Secret key; hashed with SHA-256 to derive the algorithm's key material")]
        string Key,
        [property: Description("Encryption algorithm to use")]
        EncryptionAlgorithm Algorithm = EncryptionAlgorithm.Aes256Cbc,
        [property: Description("Encoding of the returned ciphertext")]
        OutputFormat OutputFormat = OutputFormat.Base64
    );

    public sealed record Result(
        [property: PipeOutput,
                   Description("The encoded ciphertext, with the IV prepended for block cipher modes")]
        string EncryptedText
    ) : ToolResult
    {
        public Result() : this(string.Empty)
        {
        }
    }
}