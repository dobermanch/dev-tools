using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class Base64EncoderToolTests
{
    [Test]
    [Arguments("Test data 123 !", "VGVzdCBkYXRhIDEyMyAh")]
    [Arguments("Тест дата 123 !", "0KLQtdGB0YIg0LTQsNGC0LAgMTIzICE=")]
    [Arguments(
        "Character set: In case of textual data, the encoding scheme does not contain the character set, so you have to specify which character set was used during the encoding process.",
        "Q2hhcmFjdGVyIHNldDogSW4gY2FzZSBvZiB0ZXh0dWFsIGRhdGEsIHRoZSBlbmNvZGluZyBzY2hlbWUgZG9lcyBub3QgY29udGFpbiB0aGUgY2hhcmFjdGVyIHNldCwgc28geW91IGhhdmUgdG8gc3BlY2lmeSB3aGljaCBjaGFyYWN0ZXIgc2V0IHdhcyB1c2VkIGR1cmluZyB0aGUgZW5jb2RpbmcgcHJvY2Vzcy4="
    )]
    public async Task ShouldEncodeStringToBase64_WhenStringSpecified(string text, string expectedResult)
    {
        var args = new Base64EncoderTool.Args
        {
            Text = text
        };

        var result = await new Base64EncoderTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Text).IsEqualTo(expectedResult);
    }

    [Test]
    [Arguments(
        "Character set: In case of textual data, the encoding scheme does not contain the character set, so you have to specify which character set was used during the encoding process.",
        "Q2hhcmFjdGVyIHNldDogSW4gY2FzZSBvZiB0ZXh0dWFsIGRhdGEsIHRoZSBlbmNvZGluZyBzY2hl\r\nbWUgZG9lcyBub3QgY29udGFpbiB0aGUgY2hhcmFjdGVyIHNldCwgc28geW91IGhhdmUgdG8gc3Bl\r\nY2lmeSB3aGljaCBjaGFyYWN0ZXIgc2V0IHdhcyB1c2VkIGR1cmluZyB0aGUgZW5jb2RpbmcgcHJv\r\nY2Vzcy4="
    )]
    public async Task ShouldSplitEncodeStringOnNewLines_WhenInsertLineBreaksIsSet(string text, string expectedResult)
    {
        var args = new Base64EncoderTool.Args
        {
            Text = text, InsertLineBreaks = true
        }; 
        
        var result = await new Base64EncoderTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Text).IsEqualTo(expectedResult);
    }

    [Test]
    [Arguments("Тест дата 123 !", "0KLQtdGB0YIg0LTQsNGC0LAgMTIzICE")]
    public async Task ShouldEncodeToUrlSafeString_WhenUrlSafeIsSet(string text, string expectedResult)
    {
        var args = new Base64EncoderTool.Args
        {
            Text = text, UrlSafe = true
        };

        var result = await new Base64EncoderTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Text).IsEqualTo(expectedResult);
    }

    [Test]
    public async Task ShouldReturnEmptyErrorCode_WhenTextIsNotProvided()
    {
        var args = new Base64EncoderTool.Args
        {
            Text = ""
        };

        var result = await new Base64EncoderTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.TextEmpty);
    }
}
