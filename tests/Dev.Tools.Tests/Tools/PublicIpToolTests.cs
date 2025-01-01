using System.Net;
using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class PublicIpToolTests
{
    [Fact]
    public async Task WhenTextIsEmpty_ShouldReturnFailure()
    {
        var args = new PublicIpTool.Args();

        var response = await new PublicIpTool().RunAsync(args, CancellationToken.None);

        var result = true;
        if (response.IpV4 is not null)
        {
            Assert.True(IPAddress.TryParse(response.IpV4, out _), "IPv4 is wrong");
            result = false;
        }
        
        if (response.IpV6 is not null)
        {
            Assert.True(IPAddress.TryParse(response.IpV6, out _), "IPv6 is wrong");
            result = false;
        }
        
        Assert.True(result, "IP is not returned");
    }
}
