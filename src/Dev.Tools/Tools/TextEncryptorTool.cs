using System.Security.Cryptography;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "text-encryptor",
    Aliases = ["encrypt"],
    Keywords = [Keyword.Encrypt, Keyword.Text, Keyword.String],
    Categories = [Category.Crypto, Category.Security]
)]
public sealed class TextEncryptorTool : ToolBase<TextEncryptorTool.Args, TextEncryptorTool.Result>
{
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
    private static byte[] EncryptAesCbc(byte[] data, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(data, 0, data.Length);

        var result = new byte[aes.IV.Length + ciphertext.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(ciphertext, 0, result, aes.IV.Length, ciphertext.Length);
        return result;
    }

    // AES-GCM: output = Nonce[12] + Tag[16] + Ciphertext
    private static byte[] EncryptAesGcm(byte[] data, byte[] key)
    {
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
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
    private static byte[] EncryptTripleDes(byte[] data, byte[] key)
    {
        using var des = TripleDES.Create();
        des.Key = key;
        des.GenerateIV();

        using var encryptor = des.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(data, 0, data.Length);

        var result = new byte[des.IV.Length + ciphertext.Length];
        Buffer.BlockCopy(des.IV, 0, result, 0, des.IV.Length);
        Buffer.BlockCopy(ciphertext, 0, result, des.IV.Length, ciphertext.Length);
        return result;
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
        Aes128Cbc,
        Aes256Cbc,
        Aes256Gcm,
        TripleDes,
        Rc4
    }

    public enum OutputFormat
    {
        Base64,
        Hex
    }

    public sealed record Args(
        [property: PipeInput] string Text,
        string Key,
        EncryptionAlgorithm Algorithm = EncryptionAlgorithm.Aes256Cbc,
        OutputFormat OutputFormat = OutputFormat.Base64
    );

    public sealed record Result([property: PipeOutput] string EncryptedText) : ToolResult
    {
        public Result() : this(string.Empty)
        {
        }
    }
}