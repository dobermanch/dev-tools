using System.Net;
using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class IpDetailsToolTests
{
    [Test]
    public async Task ShouldReturnIpAddress()
    {
        var args = new IpDetailsTool.Args();

        var response = await new IpDetailsTool().RunAsync(args, CancellationToken.None);

        var result = false;
        if (response.IpV4 is not null)
        {
            await Assert.That(IPAddress.TryParse(response.IpV4, out _)).IsTrue();
            result = true;
        }
        
        if (response.IpV6 is not null)
        {
            await Assert.That(IPAddress.TryParse(response.IpV6, out _)).IsTrue();
            result = true;
        }
        
        await Assert.That(result).IsTrue();
    }
}
