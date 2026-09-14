using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class BaseEncodingToolTests
{
    private const string LongText =
        "Character set: In case of textual data, the encoding scheme does not contain the character set, so you have to specify which character set was used during the encoding process.";

    private static Task<BaseEncodingTool.Result> RunAsync(BaseEncodingTool.Args args)
        => new BaseEncodingTool().RunAsync(args, CancellationToken.None);

    [Test]
    [Arguments("Test data 123 !", "VGVzdCBkYXRhIDEyMyAh")]
    [Arguments("Тест дата 123 !", "0KLQtdGB0YIg0LTQsNGC0LAgMTIzICE=")]
    [Arguments(LongText, "Q2hhcmFjdGVyIHNldDogSW4gY2FzZSBvZiB0ZXh0dWFsIGRhdGEsIHRoZSBlbmNvZGluZyBzY2hlbWUgZG9lcyBub3QgY29udGFpbiB0aGUgY2hhcmFjdGVyIHNldCwgc28geW91IGhhdmUgdG8gc3BlY2lmeSB3aGljaCBjaGFyYWN0ZXIgc2V0IHdhcyB1c2VkIGR1cmluZyB0aGUgZW5jb2RpbmcgcHJvY2Vzcy4=")]
    public async Task ShouldEncodeTextAsItsUtf8Bytes(string text, string expected)
    {
        var result = await RunAsync(new BaseEncodingTool.Args(text));

        await Assert.That(result.Text).IsEqualTo(expected);
    }

    [Test]
    public async Task ShouldInsertLineBreaks_WhenEncodingWithInsertLineBreaks()
    {
        var result = await RunAsync(new BaseEncodingTool.Args(LongText, InsertLineBreaks: true));

        await Assert.That(result.Text).IsEqualTo(
            "Q2hhcmFjdGVyIHNldDogSW4gY2FzZSBvZiB0ZXh0dWFsIGRhdGEsIHRoZSBlbmNvZGluZyBzY2hl\r\nbWUgZG9lcyBub3QgY29udGFpbiB0aGUgY2hhcmFjdGVyIHNldCwgc28geW91IGhhdmUgdG8gc3Bl\r\nY2lmeSB3aGljaCBjaGFyYWN0ZXIgc2V0IHdhcyB1c2VkIGR1cmluZyB0aGUgZW5jb2RpbmcgcHJv\r\nY2Vzcy4=");
    }

    [Test]
    [Arguments(new byte[] { 0xD0, 0xA2 }, "0KI")]
    [Arguments(new byte[] { 0xFB, 0xFF }, "-_8")]
    public async Task ShouldEncodeWithTheUrlSafeAlphabetAndNoPadding_WhenUrlSafe(byte[] bytes, string expected)
    {
        var result = await RunAsync(new BaseEncodingTool.Args(ToolContent.FromBytes(bytes), UrlSafe: true));

        await Assert.That(result.Text).IsEqualTo(expected);
    }

    [Test]
    public async Task ShouldEncodeTheRawBytes_WhenContentIsBinary()
    {
        // Not valid UTF-8: text in, text out would have replaced 0xFF before encoding it.
        var result = await RunAsync(new BaseEncodingTool.Args(ToolContent.FromBytes(new byte[] { 0xFF, 0x00, 0x0A })));

        await Assert.That(result.Text).IsEqualTo("/wAK");
    }

    [Test]
    [Arguments("VGVzdCBkYXRhIDEyMyAh", "Test data 123 !")]
    [Arguments("0KLQtdGB0YIg0LTQsNGC0LAgMTIzICE=", "Тест дата 123 !")]
    [Arguments("Q2hhcmFjdGVyIHNldDogSW4gY2FzZSBvZiB0ZXh0dWFsIGRhdGEsIHRoZSBlbmNvZGluZyBzY2hl\r\nbWUgZG9lcyBub3QgY29udGFpbiB0aGUgY2hhcmFjdGVyIHNldCwgc28geW91IGhhdmUgdG8gc3Bl\r\nY2lmeSB3aGljaCBjaGFyYWN0ZXIgc2V0IHdhcyB1c2VkIGR1cmluZyB0aGUgZW5jb2RpbmcgcHJv\r\nY2Vzcy4=", LongText)]
    [Arguments("  VGVz dA==\n", "Test")]
    public async Task ShouldDecodeBase64BackToText(string base64, string expected)
    {
        var result = await RunAsync(new BaseEncodingTool.Args(base64, BaseEncodingTool.TranscodingType.Decode));

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Text).IsEqualTo(expected);
    }

    [Test]
    [Arguments("0KLQtdGB0YIg0LTQsNGC0LAgMTIzICE")]
    [Arguments("0KLQtdGB0YIg0LTQsNGC0LAgMTIzICE=")]
    public async Task ShouldDecodeTheUrlSafeAlphabetWithOrWithoutPadding_WhenUrlSafe(string base64)
    {
        var result = await RunAsync(new BaseEncodingTool.Args(base64, BaseEncodingTool.TranscodingType.Decode, UrlSafe: true));

        await Assert.That(result.Text).IsEqualTo("Тест дата 123 !");
    }

    [Test]
    public async Task ShouldGiveBackWhatWasEncoded()
    {
        var encoded = await RunAsync(new BaseEncodingTool.Args("?>> round trip <<?", UrlSafe: true));

        var decoded = await RunAsync(new BaseEncodingTool.Args(encoded.Text, BaseEncodingTool.TranscodingType.Decode, UrlSafe: true));

        await Assert.That(decoded.Text).IsEqualTo("?>> round trip <<?");
    }

    [Test]
    [Arguments(BaseEncodingTool.TranscodingType.Encode, "")]
    [Arguments(BaseEncodingTool.TranscodingType.Decode, "")]
    [Arguments(BaseEncodingTool.TranscodingType.Decode, " \n ")]
    public async Task ShouldReturnTextEmpty_WhenThereIsNothingToWorkOn(BaseEncodingTool.TranscodingType transcoding, string text)
    {
        var result = await RunAsync(new BaseEncodingTool.Args(text, transcoding));

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.TextEmpty);
    }

    [Test]
    public async Task ShouldReturnTextEmpty_WhenStreamIsEmpty()
    {
        var result = await RunAsync(new BaseEncodingTool.Args(ToolContent.FromStream(() => new MemoryStream())));

        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.TextEmpty);
    }

    [Test]
    [Arguments("!!!", false)]
    [Arguments("VGVzdA", false)]
    [Arguments("V", true)]
    public async Task ShouldReturnInputNotValid_WhenTheTextIsNotBase64(string text, bool urlSafe)
    {
        var result = await RunAsync(new BaseEncodingTool.Args(text, BaseEncodingTool.TranscodingType.Decode, urlSafe));

        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.InputNotValid);
        await Assert.That(result.Errors[0].Target).IsEqualTo(nameof(BaseEncodingTool.Args.Text));
    }
}
