using Dev.Tools.Cryptography;
using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class HashTextToolTests
{
    [Test]
    [Arguments(null, HashTextTool.HashAlgorithm.Md5)]
    [Arguments("", HashTextTool.HashAlgorithm.Md5)]
    [Arguments(null, HashTextTool.HashAlgorithm.Sha1)]
    [Arguments("", HashTextTool.HashAlgorithm.Sha1)]
    [Arguments(null, HashTextTool.HashAlgorithm.Sha256)]
    [Arguments("", HashTextTool.HashAlgorithm.Sha256)]
    [Arguments(null, HashTextTool.HashAlgorithm.Sha384)]
    [Arguments("", HashTextTool.HashAlgorithm.Sha384)]
    [Arguments(null, HashTextTool.HashAlgorithm.Sha512)]
    [Arguments("", HashTextTool.HashAlgorithm.Sha512)]
    public async Task WhenTextIsEmpty_ShouldReturnFailure(string? text, HashTextTool.HashAlgorithm algorithm)
    {
        var args = new HashTextTool.Args
        (
            Text: text!,
            Algorithm: algorithm
        );
        
        var result = await new HashTextTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Errors.Select(it => it.Code)).Contains(ErrorCode.TextEmpty);
    }

    [Test]
    [Arguments("test-text", HashTextTool.HashAlgorithm.Md5, "cf0feea200efdea7d8580c7d4ef57ced")]
    [Arguments("test-text", HashTextTool.HashAlgorithm.Sha1, "e0f1a2df4f3630efaad6aba64dbb5798bfc30866")]
    [Arguments("test-text", HashTextTool.HashAlgorithm.Sha256, "b18939c1891ba923a4d46ccf146612094490300d22e436bf5a5d6d46d99e4225")]
    [Arguments("test-text", HashTextTool.HashAlgorithm.Sha384, "d923a9e758850f9a4e6ff9d4132e61dfc2949a3ef581766500af191bc579554cef3834d326b90c66c9d805691aafcc89")]
    [Arguments("test-text", HashTextTool.HashAlgorithm.Sha512, "8c077cc8620923d614e4e5631578b7ad2b3faa28db3873eecdca2151fa3357087a9d958978bcb75be8426c7db3322c28668570edb8890f79b323eded5a8f917b")]
    public async Task WhenTextProvided_ShouldGenerateHash(string text, HashTextTool.HashAlgorithm algorithm, string expectedHash)
    {
        var args = new HashTextTool.Args
        (
            Text: text,
            Algorithm: algorithm
        );

        var result = await new HashTextTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).IsEqualTo(expectedHash);
    }

    // 0xFF 0x00 0x0A is not valid UTF-8, so these only pass if the bytes are hashed as they are.
    [Test]
    [Arguments(HashTextTool.HashAlgorithm.Md5, "99691ca21317835731e3902aeae5edf0")]
    [Arguments(HashTextTool.HashAlgorithm.Sha256, "c933d2fe5a3675b959c287c271739ac2db888cc8c0d68c1c5b58ac5b80f5d735")]
    public async Task WhenContentIsBinary_ShouldHashTheRawBytes(HashTextTool.HashAlgorithm algorithm, string expectedHash)
    {
        var args = new HashTextTool.Args(ToolContent.FromBytes(new byte[] { 0xFF, 0x00, 0x0A }), algorithm);

        var result = await new HashTextTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).IsEqualTo(expectedHash);
    }

    // What md5sum and sha256sum print for "hello\n": a stream is hashed whole, trailing line break included.
    [Test]
    [Arguments(HashTextTool.HashAlgorithm.Md5, "b1946ac92492d2347c6235b4d2611184")]
    [Arguments(HashTextTool.HashAlgorithm.Sha256, "5891b5b522d5df086d0ff0b110fbd9d21bb4fc7163af34d08286a2e846f6be03")]
    public async Task WhenContentIsAStream_ShouldHashAllOfIt(HashTextTool.HashAlgorithm algorithm, string expectedHash)
    {
        var args = new HashTextTool.Args(ToolContent.FromStream(() => new MemoryStream("hello\n"u8.ToArray())), algorithm);

        var result = await new HashTextTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).IsEqualTo(expectedHash);
    }

    [Test]
    [Arguments(HashTextTool.HashAlgorithm.Md5)]
    [Arguments(HashTextTool.HashAlgorithm.Sha256)]
    public async Task WhenStreamIsEmpty_ShouldReturnFailure(HashTextTool.HashAlgorithm algorithm)
    {
        var args = new HashTextTool.Args(ToolContent.FromStream(() => new MemoryStream()), algorithm);

        var result = await new HashTextTool(new Md5Hash()).RunAsync(args, CancellationToken.None);

        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.TextEmpty);
    }
}
