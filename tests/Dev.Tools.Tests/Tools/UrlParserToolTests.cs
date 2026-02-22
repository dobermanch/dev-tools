using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class UrlParserToolTests
{
    [Test]
    [Arguments("https://usr:pwd@tools.dev:3000/path?key1=value1&key2=value2", new []{"https", "usr","pwd", "tools.dev","3000","/path","?key1=value1&key2=value2", "key1=value1", "key2=value2"})]
    [Arguments("https://usr@tools.dev:3000/path?key1=value1", new []{"https", "usr", null, "tools.dev","3000","/path","?key1=value1", "key1=value1", null})]
    [Arguments("https://usr:@tools.dev:3000/path?key1=value1", new []{"https", "usr", null, "tools.dev","3000","/path","?key1=value1", "key1=value1", null})]
    [Arguments("https://:pwd@tools.dev:3000/path?key1=value1", new []{"https", null, "pwd", "tools.dev","3000","/path","?key1=value1", "key1=value1", null})]
    [Arguments("https://tools.dev:3000/path?key1=value1", new []{"https", null, null, "tools.dev","3000", "/path","?key1=value1", "key1=value1", null})]
    [Arguments("https://tools.dev/path?key1=value1", new []{"https", null, null, "tools.dev", "443", "/path","?key1=value1", "key1=value1", null})]
    [Arguments("https://tools.dev/path", new []{"https", null, null, "tools.dev", "443", "/path",null, null, null})]
    [Arguments("https://tools.dev", new []{"https", null, null, "tools.dev", "443", "/",null, null, null})]
    [Arguments("https://tools.dev/path?key1=", new []{"https", null, null, "tools.dev", "443", "/path","?key1=", "key1=", null})]
    [Arguments("https://tools.dev/path?=value1", new []{"https", null, null, "tools.dev", "443", "/path","?=value1", "=value1", null})]
    [Arguments("https://tools.dev/path?key1=value1&key1=value2", new []{"https", null, null, "tools.dev", "443", "/path","?key1=value1&key1=value2", "key1=value1", "key1=value2"})]
    public async Task When_Url_Provided_Should_Properly_Parse_Uri(string uri, string[] expected)
    {
        var args = new UrlParserTool.Args(uri);

        var result = await new UrlParserTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Schema).IsEqualTo(expected[0]);
        await Assert.That(result.Username).IsEqualTo(expected[1]);
        await Assert.That(result.Password).IsEqualTo(expected[2]);
        await Assert.That(result.Hostname).IsEqualTo(expected[3]);
        await Assert.That(result.Port).IsEqualTo(int.Parse(expected[4]));
        await Assert.That(result.Path).IsEqualTo(expected[5]);
        await Assert.That(result.Query).IsEqualTo(expected[6]);

        if (expected[7] != null)
        {
            await Assert.That(result.Parameters[0].Key).IsEqualTo(expected[7].Split('=').First());
            await Assert.That(result.Parameters[0].Value).IsEqualTo(expected[7].Split('=').Last());
        }
        else
        {
            await Assert.That(result.Parameters).IsEmpty();
        }
        
        if (expected[8] != null)
        {
            await Assert.That(result.Parameters[1].Key).IsEqualTo(expected[8].Split('=').First());
            await Assert.That(result.Parameters[1].Value).IsEqualTo(expected[8].Split('=').Last());
        }
    }

    [Test]
    public async Task When_Url_Not_Provided_Shout_Fail()
    {
        var args = new UrlParserTool.Args(null!);

        var result = await new UrlParserTool().RunAsync(args, CancellationToken.None);
        await Assert.That(result.ErrorCodes.First()).IsEqualTo(ErrorCode.TextEmpty);
    }
}