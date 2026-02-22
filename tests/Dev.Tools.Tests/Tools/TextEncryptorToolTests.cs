using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class TextEncryptorToolTests
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task ShouldReturnTextEmptyError_WhenTextIsNullOrEmpty(string? text)
    {
        var args = new TextEncryptorTool.Args(text!, "key");

        var result = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.TextEmpty);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task ShouldReturnInputNotValidError_WhenKeyIsNullOrEmpty(string? key)
    {
        var args = new TextEncryptorTool.Args("text", key!);

        var result = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.InputNotValid);
    }

    [Test]
    [Arguments(TextEncryptorTool.EncryptionAlgorithm.Aes128Cbc)]
    [Arguments(TextEncryptorTool.EncryptionAlgorithm.Aes256Cbc)]
    [Arguments(TextEncryptorTool.EncryptionAlgorithm.Aes256Gcm)]
    [Arguments(TextEncryptorTool.EncryptionAlgorithm.TripleDes)]
    [Arguments(TextEncryptorTool.EncryptionAlgorithm.Rc4)]
    public async Task ShouldEncryptSuccessfully_ForAllAlgorithms(TextEncryptorTool.EncryptionAlgorithm algorithm)
    {
        var args = new TextEncryptorTool.Args("Test message", "key", algorithm);

        var result = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.EncryptedText).IsNotEmpty();
    }

    [Test]
    public async Task ShouldEncryptWithAes256Cbc_AndReturnBase64ByDefault()
    {
        var args = new TextEncryptorTool.Args("Hello World", "test-key");

        var result = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.EncryptedText).IsNotEmpty();
        // Base64 should only contain valid Base64 characters
        await Assert.That(result.EncryptedText).Matches("^[A-Za-z0-9+/=]+$");
    }

    [Test]
    public async Task ShouldEncryptWithAes256Cbc_AndReturnHexWhenSpecified()
    {
        var args = new TextEncryptorTool.Args(
            "Hello World",
            "test-key",
            TextEncryptorTool.EncryptionAlgorithm.Aes256Cbc,
            TextEncryptorTool.OutputFormat.Hex
        );

        var result = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.EncryptedText).IsNotEmpty();
        // Hex should only contain hex characters (lowercase)
        await Assert.That(result.EncryptedText).Matches("^[a-f0-9]+$");
    }

    [Test]
    public async Task ShouldProduceDifferentOutput_WhenEncryptingSameTextMultipleTimes()
    {
        var args = new TextEncryptorTool.Args("Same text", "same-key");

        var result1 = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);
        var result2 = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result1.HasErrors).IsFalse();
        await Assert.That(result2.HasErrors).IsFalse();
        // Should be different due to random IV/nonce
        await Assert.That(result1.EncryptedText).IsNotEqualTo(result2.EncryptedText);
    }

    [Test]
    public async Task ShouldProduceDifferentOutput_ForDifferentKeys()
    {
        var text = "Same text";
        var args1 = new TextEncryptorTool.Args(text, "key1");
        var args2 = new TextEncryptorTool.Args(text, "key2");

        var result1 = await new TextEncryptorTool().RunAsync(args1, CancellationToken.None);
        var result2 = await new TextEncryptorTool().RunAsync(args2, CancellationToken.None);

        await Assert.That(result1.HasErrors).IsFalse();
        await Assert.That(result2.HasErrors).IsFalse();
        await Assert.That(result1.EncryptedText).IsNotEqualTo(result2.EncryptedText);
    }

    [Test]
    public async Task ShouldProduceDifferentOutput_ForDifferentAlgorithms()
    {
        var text = "Test";
        var key = "key";
        var args1 = new TextEncryptorTool.Args(
            Text: text,
            Key: key,
            Algorithm: TextEncryptorTool.EncryptionAlgorithm.Aes128Cbc
        );
        var args2 = new TextEncryptorTool.Args(
            Text: text,
            Key: key,
            Algorithm: TextEncryptorTool.EncryptionAlgorithm.Aes256Cbc
        );

        var result1 = await new TextEncryptorTool().RunAsync(args1, CancellationToken.None);
        var result2 = await new TextEncryptorTool().RunAsync(args2, CancellationToken.None);

        await Assert.That(result1.HasErrors).IsFalse();
        await Assert.That(result2.HasErrors).IsFalse();
        await Assert.That(result1.EncryptedText).IsNotEqualTo(result2.EncryptedText);
    }

    [Test]
    [Arguments("Hello 世界 🌍 Привіт")]
    [Arguments("!@#$%^&*()_+-=[]{}|;:',.<>?/~`")]
    [Arguments("   \t\n\r   ")]
    [Arguments("Line 1\nLine 2\r\nLine 3\rLine 4")]
    public async Task ShouldEncryptSpecialText_Successfully(string text)
    {
        var args = new TextEncryptorTool.Args(text, "key");

        var result = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.EncryptedText).IsNotEmpty();
    }

    [Test]
    [Arguments("x")]
    [Arguments("short")]
    public async Task ShouldEncryptShortText_Successfully(string text)
    {
        var args = new TextEncryptorTool.Args(text, "key");

        var result = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.EncryptedText).IsNotEmpty();
    }

    [Test]
    public async Task ShouldEncryptLongText_Successfully()
    {
        var longText = new string('A', 10000);
        var args = new TextEncryptorTool.Args(longText, "key");

        var result = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.EncryptedText).IsNotEmpty();
    }

    [Test]
    [Arguments("x")]
    public async Task ShouldEncryptWithShortKey_Successfully(string key)
    {
        var args = new TextEncryptorTool.Args("Text", key);

        var result = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.EncryptedText).IsNotEmpty();
    }

    [Test]
    public async Task ShouldEncryptWithLongKey_Successfully()
    {
        var longKey = new string('k', 1000);
        var args = new TextEncryptorTool.Args("Text", longKey);

        var result = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.EncryptedText).IsNotEmpty();
    }

    [Test]
    public async Task ShouldProduceValidStructure_WithAes256Cbc()
    {
        // AES-CBC output is Base64-encoded IV[16] + Ciphertext — decoded length must be >= 16
        var args = new TextEncryptorTool.Args(
            Text: "Test message",
            Key: "test-key",
            Algorithm: TextEncryptorTool.EncryptionAlgorithm.Aes256Cbc
        );

        var result = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.EncryptedText).IsNotEmpty();
        var decoded = Convert.FromBase64String(result.EncryptedText);
        await Assert.That(decoded.Length).IsGreaterThanOrEqualTo(16); // At least IV (16 bytes)
    }

    [Test]
    public async Task ShouldProduceValidBase64Output_ByDefault()
    {
        var args = new TextEncryptorTool.Args("Test", "key");

        var result = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        // Should be valid Base64 that can be decoded
        var decoded = Convert.FromBase64String(result.EncryptedText);
        await Assert.That(decoded.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ShouldProduceValidHexOutput_WhenHexFormatSpecified()
    {
        var args = new TextEncryptorTool.Args(
            Text: "Test",
            Key: "key",
            Algorithm: TextEncryptorTool.EncryptionAlgorithm.Aes256Cbc,
            OutputFormat: TextEncryptorTool.OutputFormat.Hex
        );

        var result = await new TextEncryptorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        // Should be valid hex that can be decoded
        var decoded = Convert.FromHexString(result.EncryptedText);
        await Assert.That(decoded.Length).IsGreaterThan(0);
    }
}
