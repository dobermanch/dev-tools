using System.Net;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "ip-details",
    Title = "IP Address Detector",
    Description =
        "Retrieves the caller's current public IPv4 and IPv6 addresses by querying an external lookup service. Either address is null when the corresponding protocol is unreachable.",
    Usage =
        "Reports the public address of the machine running this tool, which for a hosted server is the server's own address, not the end user's. Requires outbound network access.",
    Aliases = ["ip"],
    Keywords = [Keyword.Network, Keyword.Internet, Keyword.Ip],
    Categories = [Category.Network]
)]
public sealed class IpDetailsTool(HttpClient? httpClient = null) : ToolBase<IpDetailsTool.Args, IpDetailsTool.Result>
{
    // A single client per process: a new HttpClient per request leaks sockets.
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    private HttpClient Client => httpClient ?? SharedClient;

    protected override async Task<Result> ExecuteAsync(Args args, CancellationToken cancellationToken)
    {
        var ipV4Task = GetIpAsync("https://api.ipify.org", cancellationToken);
        var ipV6Task = GetIpAsync("https://api6.ipify.org", cancellationToken);

        var addresses = await Task.WhenAll(ipV4Task, ipV6Task);

        return new(addresses[0]?.ToString(), addresses[1]?.ToString());
    }

    private async Task<IPAddress?> GetIpAsync(string uri, CancellationToken cancellationToken)
    {
        try
        {
            var response = await Client.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string content = await response.Content.ReadAsStringAsync(cancellationToken);

            return IPAddress.TryParse(content.Trim(), out var ip) ? ip : null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation is not a lookup failure - let it surface.
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public sealed record Args;

    public sealed record Result(
        [property: Description("Public IPv4 address, or null when IPv4 lookup failed")]
        string? IpV4,
        [property: Description("Public IPv6 address, or null when IPv6 is unavailable on this network")]
        string? IpV6
    ) : ToolResult
    {
        public Result() : this(null, null)
        {
        }
    }
}