using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class JwtParserToolTests
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task ShouldReturnTextEmptyError_WhenTokenIsNullOrEmpty(string? token)
    {
        var args = new JwtParserTool.Args(token);

        var result = await new JwtParserTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.TextEmpty);
    }

    [Test]
    [Arguments("header.payload")]
    [Arguments("header.payload.signature.extra")]
    [Arguments("not-base64.eyJzdWIiOiIxMjM0NTY3ODkwIn0.signature")]
    [Arguments("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.not-base64.signature")]
    [Arguments("bm90LWpzb24.eyJzdWIiOiIxMjM0NTY3ODkwIn0.signature")]
    [Arguments("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.bm90LWpzb24.signature")]
    public async Task ShouldReturnInputNotValidError_WhenTokenIsInvalid(string token)
    {
        var args = new JwtParserTool.Args(token);

        var result = await new JwtParserTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.InputNotValid);
    }

    [Test]
    public async Task ShouldParseValidJwt_WithStandardClaims()
    {
        // JWT with header: {"alg":"HS256","typ":"JWT"}
        // payload: {"sub":"1234567890","name":"John Doe","iat":1516239022}
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        var result = await new JwtParserTool().RunAsync(new JwtParserTool.Args(token), CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Algorithm).IsEqualTo("HS256");
        await Assert.That(result.TokenType).IsEqualTo("JWT");
        await Assert.That(result.Subject).IsEqualTo("1234567890");
        await Assert.That(result.IssuedAt).IsEqualTo("2018-01-18 01:30:22 UTC");
        await Assert.That(result.Signature).IsEqualTo("SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c");
    }

    [Test]
    public async Task ShouldParseValidJwt_WithIssuerAndAudience()
    {
        // JWT with iss and aud claims
        // payload: {"iss":"https://example.com","aud":"api","sub":"user123"}
        var token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL2V4YW1wbGUuY29tIiwiYXVkIjoiYXBpIiwic3ViIjoidXNlcjEyMyJ9.sig";

        var result = await new JwtParserTool().RunAsync(new JwtParserTool.Args(token), CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Issuer).IsEqualTo("https://example.com");
        await Assert.That(result.Audience).IsEqualTo("api");
        await Assert.That(result.Subject).IsEqualTo("user123");
    }

    [Test]
    public async Task ShouldParseValidJwt_WithArrayAudience()
    {
        // JWT with array audience
        // payload: {"aud":["api1","api2"],"sub":"user123"}
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOlsiYXBpMSIsImFwaTIiXSwic3ViIjoidXNlcjEyMyJ9.sig";

        var result = await new JwtParserTool().RunAsync(new JwtParserTool.Args(token), CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Audience).IsEqualTo("api1, api2");
    }

    [Test]
    public async Task ShouldParseValidJwt_WithExpirationAndNotBefore()
    {
        // JWT with exp and nbf claims
        // payload: {"exp":1735689600,"nbf":1704067200,"sub":"user"}
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3MzU2ODk2MDAsIm5iZiI6MTcwNDA2NzIwMCwic3ViIjoidXNlciJ9.sig";

        var result = await new JwtParserTool().RunAsync(new JwtParserTool.Args(token), CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.ExpiresAt).IsEqualTo("2025-01-01 00:00:00 UTC");
        await Assert.That(result.NotBefore).IsEqualTo("2024-01-01 00:00:00 UTC");
    }

    [Test]
    public async Task ShouldParseValidJwt_WithJwtId()
    {
        // JWT with jti claim
        // payload: {"jti":"unique-id-123","sub":"user"}
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJ1bmlxdWUtaWQtMTIzIiwic3ViIjoidXNlciJ9.sig";

        var result = await new JwtParserTool().RunAsync(new JwtParserTool.Args(token), CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.JwtId).IsEqualTo("unique-id-123");
    }

    [Test]
    public async Task ShouldParseValidJwt_WithCustomClaims()
    {
        // JWT with custom claims
        // payload: {"sub":"user","role":"admin","email":"test@example.com"}
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyIiwicm9sZSI6ImFkbWluIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIn0.sig";

        var result = await new JwtParserTool().RunAsync(new JwtParserTool.Args(token), CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Claims).Count().IsEqualTo(3);
        await Assert.That(result.Claims.Any(c => c is { Key: "role", Value: "admin" })).IsTrue();
        await Assert.That(result.Claims.Any(c => c is { Key: "email", Value: "test@example.com" })).IsTrue();
    }

    [Test]
    public async Task ShouldParseValidJwt_WithMinimalPayload()
    {
        // JWT with minimal payload
        // payload: {}
        var token = "eyJhbGciOiJub25lIn0.e30.";

        var result = await new JwtParserTool().RunAsync(new JwtParserTool.Args(token), CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Algorithm).IsEqualTo("none");
        await Assert.That(result.Subject).IsNull();
        await Assert.That(result.Issuer).IsNull();
    }

    [Test]
    public async Task ShouldParseValidJwt_WithWhitespace()
    {
        var token = "  eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.sig  ";

        var result = await new JwtParserTool().RunAsync(new JwtParserTool.Args(token), CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Algorithm).IsEqualTo("HS256");
    }

    [Test]
    public async Task ShouldFormatHeaderAndPayloadJson()
    {
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIn0.sig";

        var result = await new JwtParserTool().RunAsync(new JwtParserTool.Args(token), CancellationToken.None);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.HeaderJson).Contains("\"alg\"");
        await Assert.That(result.HeaderJson).Contains("\"typ\"");
        await Assert.That(result.PayloadJson).Contains("\"sub\"");
        await Assert.That(result.PayloadJson).Contains("\"name\"");
    }
}
