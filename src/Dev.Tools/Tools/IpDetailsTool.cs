using System.Net;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "ip-details",
    Aliases = ["ip"],
    Keywords = [Keyword.Network, Keyword.Internet, Keyword.Ip],
    Categories = [Category.Network]
)]
public sealed class IpDetailsTool : ToolBase<IpDetailsTool.Args, IpDetailsTool.Result>
{
    protected override async Task<Result> ExecuteAsync(Args args, CancellationToken cancellationToken)
    {
        var ipV4Task = GetIpAsync("https://api.ipify.org", cancellationToken);
        var ipV6Task = GetIpAsync("https://api6.ipify.org", cancellationToken);

        await Task.WhenAll(ipV4Task, ipV6Task);

        return new(ipV4Task.Result?.ToString(), ipV6Task.Result?.ToString());
    }

    private async Task<IPAddress?> GetIpAsync(string uri, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string content = await response.Content.ReadAsStringAsync(cancellationToken);

            return IPAddress.TryParse(content, out var ip) ? ip : null;
        }
        catch
        {
            return null;
        }
    }

    public readonly record struct Args;

    public sealed record Result(string? IpV4, string? IpV6) : ToolResult
    {
        public Result() : this(null, null) { }
    }
}
