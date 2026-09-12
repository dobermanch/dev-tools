using System.Net;
using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class IpDetailsToolTests
{
    [Test]
    public async Task ShouldReturnIpAddresses()
    {
        using var client = new HttpClient(new StubHandler(uri => uri.Host switch
        {
            "api.ipify.org" => Respond(HttpStatusCode.OK, "203.0.113.7"),
            _ => Respond(HttpStatusCode.OK, "2001:db8::1")
        }));

        var response = await new IpDetailsTool(client).RunAsync(new IpDetailsTool.Args(), CancellationToken.None);

        await Assert.That(response.HasErrors).IsFalse();
        await Assert.That(response.IpV4).IsEqualTo("203.0.113.7");
        await Assert.That(response.IpV6).IsEqualTo("2001:db8::1");
    }

    [Test]
    public async Task WhenLookupFails_ShouldReturnNullAddresses()
    {
        using var client = new HttpClient(new StubHandler(_ => Respond(HttpStatusCode.ServiceUnavailable, string.Empty)));

        var response = await new IpDetailsTool(client).RunAsync(new IpDetailsTool.Args(), CancellationToken.None);

        await Assert.That(response.IpV4).IsNull();
        await Assert.That(response.IpV6).IsNull();
    }

    [Test]
    public async Task WhenResponseIsNotAnIpAddress_ShouldReturnNullAddresses()
    {
        using var client = new HttpClient(new StubHandler(_ => Respond(HttpStatusCode.OK, "not-an-ip")));

        var response = await new IpDetailsTool(client).RunAsync(new IpDetailsTool.Args(), CancellationToken.None);

        await Assert.That(response.IpV4).IsNull();
        await Assert.That(response.IpV6).IsNull();
    }

    [Test]
    public async Task WhenCancelled_ShouldNotReportAnIpAddress()
    {
        using var cancellation = new CancellationTokenSource();
        using var client = new HttpClient(new StubHandler(_ =>
        {
            cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
            return Respond(HttpStatusCode.OK, "203.0.113.7");
        }));

        await Assert.That(async () =>
                await new IpDetailsTool(client).RunAsync(new IpDetailsTool.Args(), cancellation.Token))
            .Throws<OperationCanceledException>();
    }

    private static HttpResponseMessage Respond(HttpStatusCode statusCode, string content) =>
        new(statusCode) { Content = new StringContent(content) };

    private sealed class StubHandler(Func<Uri, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request.RequestUri!));
    }
}
