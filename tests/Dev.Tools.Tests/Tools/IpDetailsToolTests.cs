using System.Net;
using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class IpDetailsToolTests
{
    [Fact]
    public async Task ShouldReturnIpAddress()
    {
        var args = new IpDetailsTool.Args();

        var response = await new IpDetailsTool().RunAsync(args, CancellationToken.None);

        var result = false;
        if (response.IpV4 is not null)
        {
            Assert.True(IPAddress.TryParse(response.IpV4, out _), "IPv4 is wrong");
            result = true;
        }
        
        if (response.IpV6 is not null)
        {
            Assert.True(IPAddress.TryParse(response.IpV6, out _), "IPv6 is wrong");
            result = true;
        }
        
        Assert.True(result, "IP is not returned");
    }
}
