using Dev.Tools.CodeAnalysis.Generators;

namespace Dev.Tools.CodeAnalysis.Tests.Generators;

/// <summary>
/// Tool titles and descriptions are authored in source and emitted into generated code as
/// string literals, so detailed multi-line text has to survive that trip intact.
/// </summary>
public class ToolTextEmissionTests
{
    [Test]
    [Arguments("line one\r\nline two", "line one\nline two")]
    [Arguments("old mac\rline", "old mac\nline")]
    [Arguments("already\nnormalized", "already\nnormalized")]
    [Arguments(null, "")]
    public async Task ShouldNormalizeLineEndingsSoGeneratedCodeDoesNotDependOnTheSourceFile(string? value, string expected)
    {
        await Assert.That(ToolGeneratorBase.Normalize(value)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("plain text", "plain text")]
    [Arguments("two\nlines", "two\\nlines")]
    [Arguments("a \"quoted\" word", "a \\\"quoted\\\" word")]
    [Arguments("back\\slash", "back\\\\slash")]
    [Arguments("tab\there", "tab\\there")]
    public async Task ShouldEscapeTextForAStringLiteral(string value, string expected)
    {
        await Assert.That(ToolGeneratorBase.EscapeLiteral(value)).IsEqualTo(expected);
    }

    [Test]
    public async Task ShouldEscapeABackslashBeforeTheEscapesItIntroduces()
    {
        // A naive order would turn \n into \\n and then mangle it again.
        await Assert.That(ToolGeneratorBase.EscapeLiteral("c:\\path\nnext")).IsEqualTo("c:\\\\path\\nnext");
    }
}
