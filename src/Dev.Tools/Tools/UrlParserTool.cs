using System.Web;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "url-parser",
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

    public readonly record struct Args([property: PipeInput] string? Url);

    public sealed record Result : ToolResult
    {
        public string? Schema { get; init; }
        public string? Username { get; init; }
        public string? Password { get; init; }
        public string? Hostname { get; init; }
        public int? Port { get; init; }
        public string? Path { get; init; }
        public string? Query { get; init; }
        public IList<KeyValuePair<string, string>> Parameters { get; init; } = [];
    }
}