using Dev.Tools.Core;
using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class Base64DecoderToolTests
{
    [Theory]
    [InlineData("VGVzdCBkYXRhIDEyMyAh", "Test data 123 !")]
    [InlineData("0KLQtdGB0YIg0LTQsNGC0LAgMTIzICE=", "Тест дата 123 !")]
    [InlineData(
        "Q2hhcmFjdGVyIHNldDogSW4gY2FzZSBvZiB0ZXh0dWFsIGRhdGEsIHRoZSBlbmNvZGluZyBzY2hl\r\nbWUgZG9lcyBub3QgY29udGFpbiB0aGUgY2hhcmFjdGVyIHNldCwgc28geW91IGhhdmUgdG8gc3Bl\r\nY2lmeSB3aGljaCBjaGFyYWN0ZXIgc2V0IHdhcyB1c2VkIGR1cmluZyB0aGUgZW5jb2RpbmcgcHJv\r\nY2Vzcy4gSXQgaXMgdXN1YWxseSBVVEYtOCwgYnV0IGNhbiBiZSBtYW55IG90aGVyczsgaWYgeW91\r\nIGFyZSBub3Qgc3VyZSB0aGVuIHBsYXkgd2l0aCB0aGUgYXZhaWxhYmxlIG9wdGlvbnMgb3IgdHJ5\r\nIHRoZSBhdXRvLWRldGVjdCBvcHRpb24uIFRoaXMgaW5mb3JtYXRpb24gaXMgdXNlZCB0byBjb252\r\nZXJ0IHRoZSBkZWNvZGVkIGRhdGEgdG8gb3VyIHdlYnNpdGUncyBjaGFyYWN0ZXIgc2V0IHNvIHRo\r\nYXQgYWxsIGxldHRlcnMgYW5kIHN5bWJvbHMgY2FuIGJlIGRpc3BsYXllZCBwcm9wZXJseS4gTm90\r\nZSB0aGF0IHRoaXMgaXMgaXJyZWxldmFudCBmb3IgZmlsZXMgc2luY2Ugbm8gd2ViLXNhZmUgY29u\r\ndmVyc2lvbnMgbmVlZCB0byBiZSBhcHBsaWVkIHRvIHRoZW0u",
        "Character set: In case of textual data, the encoding scheme does not contain the character set, so you have to specify which character set was used during the encoding process. It is usually UTF-8, but can be many others; if you are not sure then play with the available options or try the auto-detect option. This information is used to convert the decoded data to our website's character set so that all letters and symbols can be displayed properly. Note that this is irrelevant for files since no web-safe conversions need to be applied to them."
    )]
    public async Task ShouldDecodeString_FromBase64(string text, string expectedResult)
    {
        var args = new Base64DecoderTool.Args(text);

        var result = await new Base64DecoderTool().RunAsync(args, CancellationToken.None);

        Assert.Equal(expectedResult, result.Data);
    }

    [Fact]
    public async Task ShouldReturnEmptyErrorCode_WhenTextIsNotProvided()
    {
        var args = new Base64DecoderTool.Args("");

        var result = await new Base64DecoderTool().RunAsync(args, CancellationToken.None);

        Assert.True(result.HasErrors);
        Assert.Equal(ErrorCodes.TextEmpty, result.ErrorCodes[0]);
    }
}
