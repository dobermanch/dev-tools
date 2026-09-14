using Dev.Tools.Tests.Helpers;
using Dev.Tools.Tools;
using static Dev.Tools.Tools.EncryptionTool;

namespace Dev.Tools.Tests.Tools;

public class EncryptionToolTests
{
    private static Task<Result> RunAsync(Args args, EncryptionTool? tool = null)
        => (tool ?? new EncryptionTool()).RunAsync(args, CancellationToken.None);

    private static Task<Result> DecryptAsync(string text, string key, EncryptionAlgorithm algorithm, CiphertextFormat format = CiphertextFormat.Base64)
        => RunAsync(new Args(text, key, EncryptionOperation.Decrypt, algorithm, format));

    // Arguments

    [Test]
    [Arguments(EncryptionOperation.Encrypt, null)]
    [Arguments(EncryptionOperation.Encrypt, "")]
    [Arguments(EncryptionOperation.Decrypt, null)]
    [Arguments(EncryptionOperation.Decrypt, "")]
    public async Task ShouldReturnTextEmpty_WhenTextIsNullOrEmpty(EncryptionOperation operation, string? text)
    {
        var result = await RunAsync(new Args(text!, "key", operation));

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.TextEmpty);
    }

    [Test]
    [Arguments(EncryptionOperation.Encrypt, null)]
    [Arguments(EncryptionOperation.Encrypt, "")]
    [Arguments(EncryptionOperation.Decrypt, null)]
    [Arguments(EncryptionOperation.Decrypt, "")]
    public async Task ShouldReturnInputNotValid_WhenKeyIsNullOrEmpty(EncryptionOperation operation, string? key)
    {
        var result = await RunAsync(new Args("text", key!, operation));

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.InputNotValid);
    }

    // Encrypting

    [Test]
    [Arguments(EncryptionAlgorithm.Aes128Cbc)]
    [Arguments(EncryptionAlgorithm.Aes256Cbc)]
    [Arguments(EncryptionAlgorithm.Aes256Gcm)]
    [Arguments(EncryptionAlgorithm.TripleDes)]
    [Arguments(EncryptionAlgorithm.Rc4)]
    public async Task ShouldDecryptWhatItEncrypted_ForEveryAlgorithmAndFormat(EncryptionAlgorithm algorithm)
    {
        foreach (var format in Enum.GetValues<CiphertextFormat>())
        {
            var encrypted = await RunAsync(new Args("Test message", "key", Algorithm: algorithm, Format: format));
            var decrypted = await DecryptAsync(encrypted.Text, "key", algorithm, format);

            await Assert.That(encrypted.HasErrors).IsFalse();
            await Assert.That(decrypted.Text).IsEqualTo("Test message");
        }
    }

    [Test]
    public async Task ShouldReturnBase64ByDefault()
    {
        var result = await RunAsync(new Args("Hello World", "test-key"));

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text).Matches("^[A-Za-z0-9+/=]+$");
    }

    [Test]
    public async Task ShouldReturnLowercaseHex_WhenFormatIsHex()
    {
        var result = await RunAsync(new Args("Hello World", "test-key", Format: CiphertextFormat.Hex));

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text).Matches("^[a-f0-9]+$");
    }

    [Test]
    public async Task ShouldProduceDifferentOutput_WhenEncryptingSameTextMultipleTimes()
    {
        var args = new Args("Same text", "same-key");

        var first = await RunAsync(args);
        var second = await RunAsync(args);

        // A random IV each time.
        await Assert.That(first.Text).IsNotEqualTo(second.Text);
    }

    [Test]
    public async Task ShouldProduceDifferentOutput_ForDifferentKeysAndAlgorithms()
    {
        var tool = new EncryptionTool(new ScriptedRandomSource(0x01));

        var key1 = await RunAsync(new Args("Same text", "key1"), tool);
        var key2 = await RunAsync(new Args("Same text", "key2"), tool);
        var aes128 = await RunAsync(new Args("Same text", "key1", Algorithm: EncryptionAlgorithm.Aes128Cbc), tool);

        await Assert.That(key1.Text).IsNotEqualTo(key2.Text);
        await Assert.That(key1.Text).IsNotEqualTo(aes128.Text);
    }

    [Test]
    [Arguments("Hello 世界 🌍 Привіт")]
    [Arguments("!@#$%^&*()_+-=[]{}|;:',.<>?/~`")]
    [Arguments("   \t\n\r   ")]
    [Arguments("Line 1\nLine 2\r\nLine 3\rLine 4")]
    [Arguments("x")]
    public async Task ShouldRoundTripAnyText(string text)
    {
        var encrypted = await RunAsync(new Args(text, "key"));
        var decrypted = await DecryptAsync(encrypted.Text, "key", EncryptionAlgorithm.Aes256Cbc);

        await Assert.That(decrypted.Text).IsEqualTo(text);
    }

    [Test]
    public async Task ShouldRoundTripLongTextWithALongOrShortKey()
    {
        var text = new string('A', 10000);

        foreach (var key in new[] { "x", new string('k', 1000) })
        {
            var encrypted = await RunAsync(new Args(text, key));
            var decrypted = await DecryptAsync(encrypted.Text, key, EncryptionAlgorithm.Aes256Cbc);

            await Assert.That(decrypted.Text).IsEqualTo(text);
        }
    }

    [Test]
    [Arguments(EncryptionAlgorithm.Aes256Cbc, 16)]
    [Arguments(EncryptionAlgorithm.Aes256Gcm, 12)]
    [Arguments(EncryptionAlgorithm.TripleDes, 8)]
    public async Task ShouldTakeItsIvFromTheRandomSource(EncryptionAlgorithm algorithm, int ivBytes)
    {
        var tool = new EncryptionTool(new ScriptedRandomSource(0xAB));
        var args = new Args("hello", "secret", Algorithm: algorithm, Format: CiphertextFormat.Hex);

        var first = await RunAsync(args, tool);
        var second = await RunAsync(args, tool);

        // The IV or nonce leads the output.
        await Assert.That(first.Text).StartsWith(string.Concat(Enumerable.Repeat("ab", ivBytes)));
        // Same IV, key and text: reproducible ciphertext, which is what the seam is for.
        await Assert.That(second.Text).IsEqualTo(first.Text);
        await Assert.That((await DecryptAsync(first.Text, "secret", algorithm, CiphertextFormat.Hex)).Text).IsEqualTo("hello");
    }

    // Decrypting known ciphertext

    [Test]
    // AES-128-CBC of "Hello World", key SHA256("test-key")[..16], IV 10 20 30 ... 01.
    [Arguments("ECAwQFBgcICQoLDA0ODwAWI/uMw/NgH6VWte8Nr3i+o=", "test-key", EncryptionAlgorithm.Aes128Cbc, CiphertextFormat.Base64, "Hello World")]
    // AES-256-CBC of "Test Message", key SHA256("secret-key"), IV AA BB ... 00.
    [Arguments("qrvM3e7/ESIzRFVmd4iZAJePsizE3BduA9kft7ZmFPY=", "secret-key", EncryptionAlgorithm.Aes256Cbc, CiphertextFormat.Base64, "Test Message")]
    // AES-256-GCM of "GCM Test", key SHA256("gcm-key"), nonce 01 ... 0C: nonce[12] + tag[16] + ciphertext[8].
    [Arguments("AQIDBAUGBwgJCgsMWKxmD4QjAvUAjeVd0KeFFjlqWeGavTvQ", "gcm-key", EncryptionAlgorithm.Aes256Gcm, CiphertextFormat.Base64, "GCM Test")]
    // TripleDES-CBC of "3DES Data", key SHA256("des-key")[..24], IV 11 22 ... 88.
    [Arguments("ESIzRFVmd4jZ+4r6wK8XU6lwNaffF9dT", "des-key", EncryptionAlgorithm.TripleDes, CiphertextFormat.Base64, "3DES Data")]
    // RC4 of "RC4 Stream", key SHA256("rc4-key"), no IV.
    [Arguments("xd+i2dQopr1rZQ==", "rc4-key", EncryptionAlgorithm.Rc4, CiphertextFormat.Base64, "RC4 Stream")]
    // AES-256-CBC of "Format Test", key SHA256("key"), IV DE AD BE EF CA FE BA BE 01 23 45 67 89 AB CD EF.
    [Arguments("3q2+78r+ur4BI0VniavN79Y1GSMGVG+koZwU0FLRP6Q=", "key", EncryptionAlgorithm.Aes256Cbc, CiphertextFormat.Base64, "Format Test")]
    // AES-256-CBC of "Hex Format", key SHA256("key"), IV FE DC BA 98 76 54 32 10 F0 E1 D2 C3 B4 A5 96 87, as hex.
    [Arguments("fedcba9876543210f0e1d2c3b4a59687673f904fca77eb37e193f6604257318c", "key", EncryptionAlgorithm.Aes256Cbc, CiphertextFormat.Hex, "Hex Format")]
    [Arguments("FEDCBA9876543210F0E1D2C3B4A59687673F904FCA77EB37E193F6604257318C", "key", EncryptionAlgorithm.Aes256Cbc, CiphertextFormat.Hex, "Hex Format")]
    // AES-256-CBC of "Hello 世界 🌍", key SHA256("unicode-key").
    [Arguments("VU5JQ09ERVRFU1QAAQIDBGcrsfr/Dto1PCIJmhIHbQFhBEqGJeCTUr8IG61nZzLM", "unicode-key", EncryptionAlgorithm.Aes256Cbc, CiphertextFormat.Base64, "Hello 世界 🌍")]
    // AES-256-CBC of "!@#$%^&*()", key SHA256("special-key").
    [Arguments("U1BFQ0lBTENIUlRTVAABAug15t0ec5ZOc4OjTOKiYA0=", "special-key", EncryptionAlgorithm.Aes256Cbc, CiphertextFormat.Base64, "!@#$%^&*()")]
    public async Task ShouldDecryptKnownCiphertext(string text, string key, EncryptionAlgorithm algorithm, CiphertextFormat format, string expected)
    {
        var result = await DecryptAsync(text, key, algorithm, format);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text).IsEqualTo(expected);
    }

    [Test]
    [Arguments("not-valid-base64!!!", CiphertextFormat.Base64)]
    [Arguments("not-valid-hex-XYZ", CiphertextFormat.Hex)]
    public async Task ShouldReturnWrongFormat_WhenTheCiphertextIsNotInTheFormat(string text, CiphertextFormat format)
    {
        var result = await DecryptAsync(text, "test-key", EncryptionAlgorithm.Aes256Cbc, format);

        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.WrongFormat);
        await Assert.That(result.Errors[0].Target).IsEqualTo(nameof(Args.Text));
    }

    [Test]
    public async Task ShouldReturnFailedToDecrypt_WhenTheKeyIsWrong()
    {
        // AES-256-CBC of "Secret" with key "correct-key": another key fails PKCS#7 unpadding.
        var result = await DecryptAsync("ASNFZ4mrze8BI0VniavN7/iEcZVElDNTOqA8Q17VY9M=", "wrong-key", EncryptionAlgorithm.Aes256Cbc);

        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.FailedToDecrypt);
    }

    [Test]
    public async Task ShouldReturnFailedToDecrypt_WhenTheAlgorithmIsWrong()
    {
        // AES-256-CBC ciphertext read as AES-128-CBC: the wrong key size fails unpadding.
        var result = await DecryptAsync("qrvM3e7/ESIzRFVmd4iZAJePsizE3BduA9kft7ZmFPY=", "secret-key", EncryptionAlgorithm.Aes128Cbc);

        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.FailedToDecrypt);
    }

    [Test]
    public async Task ShouldReturnFailedToDecrypt_WhenTheCiphertextIsShorterThanTheIv()
    {
        var result = await DecryptAsync(Convert.ToBase64String(new byte[8]), "key", EncryptionAlgorithm.Aes256Cbc);

        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.FailedToDecrypt);
    }

    [Test]
    public async Task ShouldReturnFailedToDecrypt_WhenGcmCiphertextIsTampered()
    {
        var encrypted = Convert.FromBase64String("AQIDBAUGBwgJCgsMWKxmD4QjAvUAjeVd0KeFFjlqWeGavTvQ");
        encrypted[20] ^= 0xFF; // Byte 8 of the tag, which starts after the 12-byte nonce.

        var result = await DecryptAsync(Convert.ToBase64String(encrypted), "gcm-key", EncryptionAlgorithm.Aes256Gcm);

        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.FailedToDecrypt);
    }
}
