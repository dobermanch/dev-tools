using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class SafeUrlTranscoderToolTests
{
    [Test]
    [Arguments("Hello world :)", SafeUrlTranscoderTool.TranscodingType.Encode, "Hello+world+%3a)")]
    [Arguments("https://example.com?value=Hello world :)", SafeUrlTranscoderTool.TranscodingType.Encode, "https%3a%2f%2fexample.com%3fvalue%3dHello+world+%3a)")]
    [Arguments("Hello+world+%3a)", SafeUrlTranscoderTool.TranscodingType.Decode, "Hello world :)")]
    [Arguments("https%3a%2f%2fexample.com%3fvalue%3dHello+world+%3a)", SafeUrlTranscoderTool.TranscodingType.Decode, "https://example.com?value=Hello world :)")]
    public async Task ShouldTranscodeToSafeUrlString(string input, SafeUrlTranscoderTool.TranscodingType type, string expectedResult)
    {
        var args = new SafeUrlTranscoderTool.Args(input, type);

        var result = await new SafeUrlTranscoderTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Url).IsEqualTo(expectedResult);
    }

    [Test]
    public async Task ShouldReturnEmptyErrorCode_WhenTextIsNotProvided()
    {
        var args = new SafeUrlTranscoderTool.Args("", SafeUrlTranscoderTool.TranscodingType.Encode);

        var result = await new SafeUrlTranscoderTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.TextEmpty);
    }
}