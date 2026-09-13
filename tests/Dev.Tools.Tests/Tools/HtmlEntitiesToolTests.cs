using Dev.Tools.Tools;
using static Dev.Tools.Tools.HtmlEntitiesTool.TranscodingType;

namespace Dev.Tools.Tests.Tools;

public class HtmlEntitiesToolTests
{
    private static Task<HtmlEntitiesTool.Result> RunAsync(
        string? text,
        HtmlEntitiesTool.TranscodingType transcoding,
        bool encodeNonAscii = false)
        => new HtmlEntitiesTool().RunAsync(
            new HtmlEntitiesTool.Args(text, transcoding, encodeNonAscii),
            CancellationToken.None);

    [Test]
    [Arguments("<a href=\"x\">Tom & 'Jerry'</a>", "&lt;a href=&quot;x&quot;&gt;Tom &amp; &#39;Jerry&#39;&lt;/a&gt;")]
    [Arguments("plain text", "plain text")]
    [Arguments("&amp;", "&amp;amp;")]
    [Arguments("café \U0001F44D", "café \U0001F44D")]
    public async Task ShouldEscapeOnlyTheCharactersHtmlGivesAMeaningTo(string text, string expected)
    {
        var result = await RunAsync(text, Encode);

        await Assert.That(result.Text).IsEqualTo(expected);
    }

    [Test]
    [Arguments("café \U0001F44D <b>", "caf&#233; &#128077; &lt;b&gt;")]
    [Arguments("Привіт", "&#1055;&#1088;&#1080;&#1074;&#1110;&#1090;")]
    public async Task ShouldWriteNonAsciiAsNumericReferences_WhenAsked(string text, string expected)
    {
        var result = await RunAsync(text, Encode, encodeNonAscii: true);

        await Assert.That(result.Text).IsEqualTo(expected);
    }

    [Test]
    [Arguments("&lt;p class=&quot;x&quot;&gt;Tom &amp; Jerry&lt;/p&gt;", "<p class=\"x\">Tom & Jerry</p>")]
    [Arguments("&#39;&#x27;&#X27;", "'''")]
    [Arguments("caf&#233; &#128077;&#x1F44D;", "café \U0001F44D\U0001F44D")]
    [Arguments("&copy; &eacute; &nbsp;", "© é  ")]
    [Arguments("&apos;", "'")]
    public async Task ShouldDecodeNamedAndNumericReferences(string text, string expected)
    {
        var result = await RunAsync(text, Decode);

        await Assert.That(result.Text).IsEqualTo(expected);
    }

    [Test]
    [Arguments("&bogus; & 5 &lt 3")]
    [Arguments("plain text")]
    public async Task ShouldLeaveTextThatIsNotAReference(string text)
    {
        var result = await RunAsync(text, Decode);

        await Assert.That(result.Text).IsEqualTo(text);
    }

    [Test]
    public async Task ShouldIgnoreEncodeNonAscii_WhenDecoding()
    {
        var result = await RunAsync("caf&#233; é", Decode, encodeNonAscii: true);

        await Assert.That(result.Text).IsEqualTo("café é");
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ShouldGiveTheTextBack_WhenEncodedAndDecoded(bool encodeNonAscii)
    {
        const string text = "<Привіт & 'світ' \"\U0001F44D\">";

        var encoded = await RunAsync(text, Encode, encodeNonAscii);
        var decoded = await RunAsync(encoded.Text, Decode);

        await Assert.That(decoded.Text).IsEqualTo(text);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task ShouldReturnTextEmpty_WhenTextIsNotProvided(string? text)
    {
        var result = await RunAsync(text, Encode);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.TextEmpty);
        await Assert.That(result.Errors[0].Target).IsEqualTo(nameof(HtmlEntitiesTool.Args.Text));
    }
}
