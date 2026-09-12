using System.Web;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "url-parser",
    Title = "URL Parser",
    Description =
        "Parses an absolute URL into its components: scheme, credentials, hostname, port, path, query string and decoded query parameters. Relative URLs are rejected.",
    Aliases = [],
    Keywords = [Keyword.Decode, Keyword.Internet, Keyword.Web, Keyword.Network, Keyword.Url, Keyword.String],
    Categories = [Category.Web]
)]
public sealed class UrlParserTool : ToolBase<UrlParserTool.Args, UrlParserTool.Result>
{
    protected override Result Execute(Args args)
    {
        try
        {
            if (string.IsNullOrEmpty(args.Url))
            {
                throw new ToolException(ErrorCode.TextEmpty);
            }

            var uri = new Uri(args.Url);

            string? userName = null;
            string? password = null;
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var parts = uri.UserInfo.Split(':');
                userName = parts.Length > 0 && parts[0] != "" ? parts[0] : null;
                password = parts.Length > 1 && parts[1] != "" ? parts[1] : null;
            }

            var pairs = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(uri.Query))
            {
                var nameValues = HttpUtility.ParseQueryString(uri.Query);
                foreach (var key in nameValues.AllKeys)
                {
                    var values = nameValues.GetValues(key);
                    if (values != null)
                    {
                        foreach (var value in values)
                        {
                            pairs.Add(new KeyValuePair<string, string>(key ?? string.Empty, value));
                        }
                    }
                    else if (key != null)
                    {
                        pairs.Add(new KeyValuePair<string, string>(key, string.Empty));
                    }
                }
            }

            return new()
            {
                Schema = uri.Scheme,
                Username = userName,
                Password = password,
                Hostname = uri.Host,
                Port = uri.Port,
                Path = string.IsNullOrEmpty(uri.Query) ? uri.PathAndQuery : uri.PathAndQuery.Replace(uri.Query, string.Empty),
                Query = string.IsNullOrEmpty(uri.Query) ? null : uri.Query,
                Parameters = pairs
            };
        }
        catch (FormatException)
        {
            throw new ToolException(ErrorCode.InputNotValid);
        }
    }

    public sealed record Args(
        [property: PipeInput,
                   DisplayName("URL"),
                   Description("Absolute URL to parse, including the scheme")]
        string Url
    );

    public sealed record Result : ToolResult
    {
        [DisplayName("Scheme")]
        [Description("URL scheme (http, https, ftp, ...)")]
        public string? Schema { get; init; }

        [Description("Username from the URL, or null when there are no credentials")]
        public string? Username { get; init; }

        [Description("Password from the URL, or null when there are no credentials")]
        public string? Password { get; init; }

        [Description("Domain name or IP address")]
        public string? Hostname { get; init; }

        [Description("Port number; the scheme's default port when none was given")]
        public int? Port { get; init; }

        [Description("Path component, without the query string")]
        public string? Path { get; init; }

        [DisplayName("Query String")]
        [Description("Raw query string including the leading '?', or null when there is none")]
        public string? Query { get; init; }

        [Description("Decoded query parameters as key-value pairs; a repeated key appears once per value")]
        public IList<KeyValuePair<string, string>> Parameters { get; init; } = [];
    }
}