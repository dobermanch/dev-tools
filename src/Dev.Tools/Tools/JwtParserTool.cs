using System.Text.Json;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "jwt-parser",
    Title = "JWT Parser",
    Description =
        """
        Decodes a JSON Web Token into its header, payload claims and signature.

        The token is only decoded, never verified: the signature is returned as-is and no key is
        checked, so a token that parses successfully is not proof that it is authentic or unexpired.
        """,
    Usage =
        "Never treat the claims returned here as trusted - decoding is not verification. Payloads routinely contain personal data and access scopes, so avoid echoing a whole token back where it may be logged.",
    Aliases = ["jwt"],
    Keywords = [Keyword.Decode, Keyword.Token, Keyword.Json, Keyword.String, Keyword.Web, Keyword.Jwt],
    Categories = [Category.Security, Category.Web]
)]
public sealed class JwtParserTool : ToolBase<JwtParserTool.Args, JwtParserTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Token))
        {
            throw new ToolException(ErrorCode.TextEmpty);
        }

        var parts = args.Token.Trim().Split('.');
        if (parts.Length != 3)
        {
            throw new ToolException(ErrorCode.InputNotValid);
        }

        try
        {
            var headerJson = DecodeBase64Url(parts[0]);
            var payloadJson = DecodeBase64Url(parts[1]);

            using var headerDoc = JsonDocument.Parse(headerJson);
            using var payloadDoc = JsonDocument.Parse(payloadJson);

            return new()
            {
                HeaderJson = FormatJson(headerJson),
                PayloadJson = FormatJson(payloadJson),
                Signature = parts[2],
                Algorithm = GetString(headerDoc, "alg"),
                TokenType = GetString(headerDoc, "typ"),
                Issuer = GetString(payloadDoc, "iss"),
                Subject = GetString(payloadDoc, "sub"),
                Audience = GetAudience(payloadDoc),
                IssuedAt = GetUnixDateTime(payloadDoc, "iat"),
                NotBefore = GetUnixDateTime(payloadDoc, "nbf"),
                ExpiresAt = GetUnixDateTime(payloadDoc, "exp"),
                JwtId = GetString(payloadDoc, "jti"),
                Claims = GetAllClaims(payloadDoc)
            };
        }
        catch (FormatException)
        {
            throw new ToolException(ErrorCode.InputNotValid);
        }
        catch (JsonException)
        {
            throw new ToolException(ErrorCode.InputNotValid);
        }
    }

    private static string DecodeBase64Url(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        base64 += (base64.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => ""
        };
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    private static string FormatJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string? GetString(JsonDocument doc, string key)
        => doc.RootElement.TryGetProperty(key, out var val) ? val.ToString() : null;

    private static string? GetAudience(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("aud", out var val))
        {
            return null;
        }
        
        return val.ValueKind == JsonValueKind.Array
            ? string.Join(", ", val.EnumerateArray().Select(v => v.ToString()))
            : val.ToString();
    }

    private static string? GetUnixDateTime(JsonDocument doc, string key)
    {
        if (!doc.RootElement.TryGetProperty(key, out var val))
        {
            return null;
        }

        return val.TryGetInt64(out var unix) 
            ? DateTimeOffset.FromUnixTimeSeconds(unix).ToString("yyyy-MM-dd HH:mm:ss UTC") 
            : val.ToString();
    }

    private static IList<KeyValuePair<string, string>> GetAllClaims(JsonDocument doc)
        => doc.RootElement.EnumerateObject()
            .Select(p => new KeyValuePair<string, string>(p.Name, p.Value.ToString()))
            .ToList();

    public sealed record Args(
        [property: PipeInput,
                   DisplayName("JWT Token"),
                   Description("JWT to parse: three Base64url-encoded parts separated by dots")]
        string? Token
    );

    public sealed record Result : ToolResult
    {
        [DisplayName("Header")]
        [Description("Decoded and formatted header JSON, carrying the algorithm and token type")]
        public string? HeaderJson { get; init; }

        [DisplayName("Payload")]
        [Description("Decoded and formatted payload JSON, carrying all claims")]
        public string? PayloadJson { get; init; }

        [Description("Raw Base64url-encoded signature, the third part of the token; not verified")]
        public string? Signature { get; init; }

        [Description("Signing algorithm from the header (alg claim)")]
        public string? Algorithm { get; init; }

        [Description("Token type from the header (typ claim)")]
        public string? TokenType { get; init; }

        [Description("Issuer of the token (iss claim)")]
        public string? Issuer { get; init; }

        [Description("Subject of the token (sub claim)")]
        public string? Subject { get; init; }

        [Description("Intended recipients of the token (aud claim)")]
        public string? Audience { get; init; }

        [Description("Time the token was issued (iat claim)")]
        public string? IssuedAt { get; init; }

        [Description("Time before which the token is not valid (nbf claim)")]
        public string? NotBefore { get; init; }

        [Description("Expiration time of the token (exp claim)")]
        public string? ExpiresAt { get; init; }

        [DisplayName("JWT ID")]
        [Description("Unique identifier for the token (jti claim)")]
        public string? JwtId { get; init; }

        [Description("All payload claims as key-value pairs")]
        public IList<KeyValuePair<string, string>> Claims { get; init; } = [];
    }
}