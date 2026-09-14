using System.Security.Cryptography;
using Dev.Tools.Cryptography;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "encryption",
    Title = "Text Encryption",
    Description =
        """
        Encrypts text with a symmetric algorithm, or decrypts text this tool encrypted.

        The key is turned into algorithm-sized key material with a single SHA-256 pass, so the result is
        only as strong as the key itself. Encrypting returns the ciphertext as Base64 or hex with a random
        IV in front of it - the nonce and tag for AES-GCM - and decrypting reads that layout back, so the
        key, algorithm and format must be the ones used to encrypt. A wrong key usually fails outright, but
        only AES-GCM authenticates the ciphertext: the other algorithms can return meaningless text.
        """,
    Usage =
        "This is a developer utility, not a key-management system: there is no password stretching, no salt and no integrity check outside GCM, so it is not suitable for protecting data at rest. It only decrypts its own output - ciphertext from another program will usually fail, because the IV placement and key derivation differ. A decryption failure means the key, algorithm or format did not match; it does not say which.",
    Aliases = ["cipher"],
    Keywords = [Keyword.Encrypt, Keyword.Decode, Keyword.Text, Keyword.String],
    Categories = [Category.Crypto, Category.Security]
)]
public sealed class EncryptionTool(IRandomSource? random = null)
    : ToolBase<EncryptionTool.Args, EncryptionTool.Result>
{
    // The IVs and the nonce must be unpredictable, which the default source guarantees. A test may
    // substitute a fixed one to get reproducible ciphertext; nothing else should.
    private readonly IRandomSource _random = random ?? CryptographicRandomSource.Instance;

    protected override Result Execute(Args args)
    {
        // Both are checked before either is reported, so a caller missing both hears about both.
        var errors = new List<ToolError>();
        if (string.IsNullOrEmpty(args.Text))
        {
            errors.Add(new ToolError(ErrorCode.TextEmpty, nameof(Args.Text)));
        }

        if (string.IsNullOrEmpty(args.Key))
        {
            errors.Add(new ToolError(ErrorCode.InputNotValid, nameof(Args.Key)));
        }

        if (errors.Count > 0)
        {
            throw new ToolException(errors);
        }

        var key = SHA256.HashData(Encoding.UTF8.GetBytes(args.Key));

        return new Result(args.Operation == EncryptionOperation.Decrypt ? Decrypt(args, key) : Encrypt(args, key));
    }

    private string Encrypt(Args args, byte[] key)
    {
        try
        {
            var data = Encoding.UTF8.GetBytes(args.Text);
            var encrypted = args.Algorithm switch
            {
                EncryptionAlgorithm.Aes128Cbc => EncryptCbc(Aes.Create(), data, key[..16]),
                EncryptionAlgorithm.Aes256Cbc => EncryptCbc(Aes.Create(), data, key),
                EncryptionAlgorithm.Aes256Gcm => EncryptAesGcm(data, key),
                EncryptionAlgorithm.TripleDes => EncryptCbc(TripleDES.Create(), data, key[..24]),
                EncryptionAlgorithm.Rc4 => Rc4(data, key),
                _ => throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Algorithm))
            };

            return args.Format == CiphertextFormat.Hex
                ? Convert.ToHexStringLower(encrypted)
                : Convert.ToBase64String(encrypted);
        }
        catch (ToolException)
        {
            throw;
        }
        catch
        {
            throw new ToolException(ErrorCode.FailedToEncrypt);
        }
    }

    private static string Decrypt(Args args, byte[] key)
    {
        byte[] data;
        try
        {
            data = args.Format == CiphertextFormat.Hex
                ? Convert.FromHexString(args.Text)
                : Convert.FromBase64String(args.Text);
        }
        catch (FormatException)
        {
            throw new ToolException(ErrorCode.WrongFormat, nameof(Args.Text));
        }

        try
        {
            var decrypted = args.Algorithm switch
            {
                EncryptionAlgorithm.Aes128Cbc => DecryptCbc(Aes.Create(), data, key[..16]),
                EncryptionAlgorithm.Aes256Cbc => DecryptCbc(Aes.Create(), data, key),
                EncryptionAlgorithm.Aes256Gcm => DecryptAesGcm(data, key),
                EncryptionAlgorithm.TripleDes => DecryptCbc(TripleDES.Create(), data, key[..24]),
                EncryptionAlgorithm.Rc4 => Rc4(data, key),
                _ => throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Algorithm))
            };

            return Encoding.UTF8.GetString(decrypted);
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

    // CBC, AES or TripleDES: IV[block size] + ciphertext.
    private byte[] EncryptCbc(SymmetricAlgorithm cipher, byte[] data, byte[] key)
    {
        using (cipher)
        {
            cipher.Key = key;
            cipher.IV = RandomBytes(cipher.BlockSize / 8);

            using var encryptor = cipher.CreateEncryptor();
            return [.. cipher.IV, .. encryptor.TransformFinalBlock(data, 0, data.Length)];
        }
    }

    private static byte[] DecryptCbc(SymmetricAlgorithm cipher, byte[] data, byte[] key)
    {
        using (cipher)
        {
            var ivSize = cipher.BlockSize / 8;
            cipher.Key = key;
            cipher.IV = data[..ivSize];

            using var decryptor = cipher.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, ivSize, data.Length - ivSize);
        }
    }

    // AES-GCM: nonce[12] + tag[16] + ciphertext.
    private byte[] EncryptAesGcm(byte[] data, byte[] key)
    {
        var nonce = RandomBytes(AesGcm.NonceByteSizes.MaxSize);
        var ciphertext = new byte[data.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using var aesGcm = new AesGcm(key, tag.Length);
        aesGcm.Encrypt(nonce, data, ciphertext, tag);

        return [.. nonce, .. tag, .. ciphertext];
    }

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

    private byte[] RandomBytes(int count)
    {
        var bytes = new byte[count];
        _random.Fill(bytes);
        return bytes;
    }

    // RC4 is a stream cipher with no IV, and the same operation both ways.
    private static byte[] Rc4(byte[] data, byte[] key)
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

    public enum EncryptionOperation
    {
        [Description("Encrypt plain text")]
        Encrypt,

        [Description("Decrypt text this tool encrypted")]
        Decrypt
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

    public enum CiphertextFormat
    {
        [Description("Base64")]
        Base64,

        [Description("Hexadecimal, written in lowercase and read in either case")]
        Hex
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Plain text to encrypt, or ciphertext in the chosen format to decrypt")]
        string Text,
        [property: DisplayName("Secret Key"),
                   Description("Secret key, hashed with SHA-256 to derive the algorithm's key material; decrypting needs the key that encrypted")]
        string Key,
        [property: Description("Whether to encrypt or decrypt the text")]
        EncryptionOperation Operation = EncryptionOperation.Encrypt,
        [property: Description("Encryption algorithm; decrypting needs the one that encrypted")]
        EncryptionAlgorithm Algorithm = EncryptionAlgorithm.Aes256Cbc,
        [property: Description("Encoding of the ciphertext: what encrypting returns and decrypting reads")]
        CiphertextFormat Format = CiphertextFormat.Base64
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("Result"),
                   Description("The encoded ciphertext with the IV in front of it, or the recovered plain text")]
        string Text
    ) : ToolResult
    {
        public Result() : this(string.Empty)
        {
        }
    }
}
