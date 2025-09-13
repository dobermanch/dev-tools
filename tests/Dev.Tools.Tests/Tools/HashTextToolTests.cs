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
        {
            Text = text,
            Algorithm = algorithm
        };
        
        var result = await new HashTextTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes).Contains(ErrorCode.TextEmpty);
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
        {
            Text = text,
            Algorithm = algorithm
        };

        var result = await new HashTextTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).IsEqualTo(expectedHash);
    }
}
