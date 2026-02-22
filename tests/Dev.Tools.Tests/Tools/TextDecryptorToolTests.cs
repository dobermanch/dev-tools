using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class TextDecryptorToolTests
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task ShouldReturnTextEmptyError_WhenEncryptedTextIsNullOrEmpty(string? encryptedText)
    {
        var args = new TextDecryptorTool.Args(encryptedText!, "key");

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.TextEmpty);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task ShouldReturnInputNotValidError_WhenKeyIsNullOrEmpty(string? key)
    {
        var args = new TextDecryptorTool.Args("encrypted", key!);

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.InputNotValid);
    }

    [Test]
    [Arguments("not-valid-base64!!!", TextDecryptorTool.InputFormat.Base64)]
    [Arguments("not-valid-hex-XYZ", TextDecryptorTool.InputFormat.Hex)]
    public async Task ShouldReturnWrongFormatError_WhenInputFormatIsInvalid(string invalidInput, TextDecryptorTool.InputFormat format)
    {
        var args = new TextDecryptorTool.Args(
            EncryptedText: invalidInput,
            Key: "test-key",
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.Aes256Cbc,
            InputFormat: format
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.WrongFormat);
    }

    [Test]
    public async Task ShouldDecryptAes128Cbc_Successfully()
    {
        // AES-128-CBC encryption of "Hello World" with key "test-key"
        // Key: SHA256("test-key")[..16], IV: { 0x10,0x20,...,0x01 }
        // Format: IV[16] + Ciphertext
        var encryptedBase64 = "ECAwQFBgcICQoLDA0ODwAWI/uMw/NgH6VWte8Nr3i+o=";
        var key = "test-key";

        var args = new TextDecryptorTool.Args(
            EncryptedText: encryptedBase64,
            Key: key,
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.Aes128Cbc
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.DecryptedText).IsEqualTo("Hello World");
    }

    [Test]
    public async Task ShouldDecryptAes256Cbc_Successfully()
    {
        // AES-256-CBC encryption of "Test Message" with key "secret-key"
        // Key: SHA256("secret-key"), IV: { 0xAA,0xBB,...,0x00 }
        // Format: IV[16] + Ciphertext
        var encryptedBase64 = "qrvM3e7/ESIzRFVmd4iZAJePsizE3BduA9kft7ZmFPY=";
        var key = "secret-key";

        var args = new TextDecryptorTool.Args(
            EncryptedText: encryptedBase64,
            Key: key,
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.Aes256Cbc
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.DecryptedText).IsEqualTo("Test Message");
    }

    [Test]
    public async Task ShouldDecryptAes256Gcm_Successfully()
    {
        // AES-256-GCM encryption of "GCM Test" with key "gcm-key"
        // Key: SHA256("gcm-key"), Nonce: { 0x01..0x0C }
        // Format: Nonce[12] + Tag[16] + Ciphertext[8]
        var encryptedBase64 = "AQIDBAUGBwgJCgsMWKxmD4QjAvUAjeVd0KeFFjlqWeGavTvQ";
        var key = "gcm-key";

        var args = new TextDecryptorTool.Args(
            EncryptedText: encryptedBase64,
            Key: key,
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.Aes256Gcm
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.DecryptedText).IsEqualTo("GCM Test");
    }

    [Test]
    public async Task ShouldDecryptTripleDes_Successfully()
    {
        // TripleDES-CBC encryption of "3DES Data" with key "des-key"
        // Key: SHA256("des-key")[..24], IV: { 0x11,0x22,...,0x88 }
        // Format: IV[8] + Ciphertext
        var encryptedBase64 = "ESIzRFVmd4jZ+4r6wK8XU6lwNaffF9dT";
        var key = "des-key";

        var args = new TextDecryptorTool.Args(
            EncryptedText: encryptedBase64,
            Key: key,
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.TripleDes
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.DecryptedText).IsEqualTo("3DES Data");
    }

    [Test]
    public async Task ShouldDecryptRc4_Successfully()
    {
        // RC4 encryption of "RC4 Stream" with key "rc4-key"
        // Key: SHA256("rc4-key"), no IV (stream cipher)
        // Format: Ciphertext only
        var encryptedBase64 = "xd+i2dQopr1rZQ==";
        var key = "rc4-key";

        var args = new TextDecryptorTool.Args(
            EncryptedText: encryptedBase64,
            Key: key,
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.Rc4
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.DecryptedText).IsEqualTo("RC4 Stream");
    }

    [Test]
    public async Task ShouldDecryptWithBase64Format_Successfully()
    {
        // AES-256-CBC encryption of "Format Test" with key "key"
        // Key: SHA256("key"), IV: { 0xDE,0xAD,0xBE,0xEF,0xCA,0xFE,0xBA,0xBE,0x01,0x23,0x45,0x67,0x89,0xAB,0xCD,0xEF }
        // Format: IV[16] + Ciphertext, encoded as Base64
        var encryptedBase64 = "3q2+78r+ur4BI0VniavN79Y1GSMGVG+koZwU0FLRP6Q=";
        var key = "key";

        var args = new TextDecryptorTool.Args(
            EncryptedText: encryptedBase64,
            Key: key,
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.Aes256Cbc,
            InputFormat: TextDecryptorTool.InputFormat.Base64
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.DecryptedText).IsEqualTo("Format Test");
    }

    [Test]
    public async Task ShouldDecryptWithHexFormat_Successfully()
    {
        // AES-256-CBC encryption of "Hex Format" with key "key"
        // Key: SHA256("key"), IV: { 0xFE,0xDC,0xBA,0x98,0x76,0x54,0x32,0x10,0xF0,0xE1,0xD2,0xC3,0xB4,0xA5,0x96,0x87 }
        // Format: IV[16] + Ciphertext, encoded as lowercase hex
        var encryptedHex = "fedcba9876543210f0e1d2c3b4a59687673f904fca77eb37e193f6604257318c";
        var key = "key";

        var args = new TextDecryptorTool.Args(
            EncryptedText: encryptedHex,
            Key: key,
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.Aes256Cbc,
            InputFormat: TextDecryptorTool.InputFormat.Hex
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.DecryptedText).IsEqualTo("Hex Format");
    }

    [Test]
    public async Task ShouldDecryptUnicodeText_Successfully()
    {
        // AES-256-CBC encryption of "Hello 世界 🌍" with key "unicode-key"
        // Key: SHA256("unicode-key"), IV: { 0x55,0x4E,0x49,0x43,0x4F,0x44,0x45,0x54,0x45,0x53,0x54,0x00,0x01,0x02,0x03,0x04 }
        // Format: IV[16] + Ciphertext
        var encryptedBase64 = "VU5JQ09ERVRFU1QAAQIDBGcrsfr/Dto1PCIJmhIHbQFhBEqGJeCTUr8IG61nZzLM";
        var key = "unicode-key";

        var args = new TextDecryptorTool.Args(
            EncryptedText: encryptedBase64,
            Key: key,
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.Aes256Cbc
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.DecryptedText).IsEqualTo("Hello 世界 🌍");
    }

    [Test]
    public async Task ShouldDecryptSpecialCharacters_Successfully()
    {
        // AES-256-CBC encryption of "!@#$%^&*()" with key "special-key"
        // Key: SHA256("special-key"), IV: { 0x53,0x50,0x45,0x43,0x49,0x41,0x4C,0x43,0x48,0x52,0x54,0x53,0x54,0x00,0x01,0x02 }
        // Format: IV[16] + Ciphertext
        var encryptedBase64 = "U1BFQ0lBTENIUlRTVAABAug15t0ec5ZOc4OjTOKiYA0=";
        var key = "special-key";

        var args = new TextDecryptorTool.Args(
            EncryptedText: encryptedBase64,
            Key: key,
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.Aes256Cbc
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.DecryptedText).IsEqualTo("!@#$%^&*()");
    }

    [Test]
    public async Task ShouldReturnFailedToDecryptError_WhenWrongKeyUsed()
    {
        // AES-256-CBC encryption of "Secret" with key "correct-key"
        // Key: SHA256("correct-key"), IV: { 0x01,0x23,...,0xEF }
        // Decrypting with "wrong-key" produces a different AES key and fails PKCS7 unpadding
        var encryptedBase64 = "ASNFZ4mrze8BI0VniavN7/iEcZVElDNTOqA8Q17VY9M=";
        var wrongKey = "wrong-key";

        var args = new TextDecryptorTool.Args(
            EncryptedText: encryptedBase64,
            Key: wrongKey,
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.Aes256Cbc
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.FailedToDecrypt);
    }

    [Test]
    public async Task ShouldReturnFailedToDecryptError_WhenWrongAlgorithmUsed()
    {
        // AES-256-CBC encryption of "Test Message" with key "secret-key"
        // Attempting to decrypt with AES-128-CBC fails: wrong key size causes bad padding
        var encryptedBase64 = "qrvM3e7/ESIzRFVmd4iZAJePsizE3BduA9kft7ZmFPY=";
        var key = "secret-key";

        var args = new TextDecryptorTool.Args(
            EncryptedText: encryptedBase64,
            Key: key,
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.Aes128Cbc
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.FailedToDecrypt);
    }

    [Test]
    public async Task ShouldReturnFailedToDecryptError_WhenEncryptedDataIsTooShort()
    {
        // 8 bytes decoded — too short to contain even the AES-CBC IV (16 bytes)
        var tooShortEncrypted = Convert.ToBase64String(new byte[8]);

        var args = new TextDecryptorTool.Args(
            EncryptedText: tooShortEncrypted,
            Key: "key",
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.Aes256Cbc
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.FailedToDecrypt);
    }

    [Test]
    public async Task ShouldReturnFailedToDecryptError_WhenGcmDataIsCorrupted()
    {
        // AES-256-GCM encryption of "GCM Test" with key "gcm-key"
        // Format: Nonce[12] + Tag[16] + Ciphertext[8]
        // Flipping a bit in the tag causes GCM authentication to fail
        var encryptedBase64 = "AQIDBAUGBwgJCgsMWKxmD4QjAvUAjeVd0KeFFjlqWeGavTvQ";
        var encrypted = Convert.FromBase64String(encryptedBase64);
        encrypted[20] ^= 0xFF; // Corrupt byte 8 of the tag (offset 12+8=20)
        var corruptedData = Convert.ToBase64String(encrypted);

        var args = new TextDecryptorTool.Args(
            EncryptedText: corruptedData,
            Key: "gcm-key",
            Algorithm: TextDecryptorTool.EncryptionAlgorithm.Aes256Gcm
        );

        var result = await new TextDecryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.FailedToDecrypt);
    }
}
