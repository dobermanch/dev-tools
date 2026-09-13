using Dev.Tools.Console.Core;

namespace Dev.Tools.Console.Tests;

public class CliNamingTests
{
    [Test]
    [Arguments("Key", "key")]
    [Arguments("UrlSafe", "url-safe")]
    [Arguments("EncodeNonAscii", "encode-non-ascii")]
    [Arguments("TreatAsStandardHexCodes", "treat-as-standard-hex-codes")]
    [Arguments("Utf8Bytes", "utf8-bytes")]
    [Arguments("IPAddress", "ip-address")]
    [Arguments("ParseJSON", "parse-json")]
    public async Task ShouldSpellPropertyNamesInKebabCase(string propertyName, string expected)
    {
        await Assert.That(CliNaming.Kebab(propertyName)).IsEqualTo(expected);
    }
}
