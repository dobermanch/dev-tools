using Dev.Tools.Tools;
using static Dev.Tools.Tools.CharacterViewerTool.ViewType;

namespace Dev.Tools.Tests.Tools;

public class CharacterViewerToolTests
{
    [Test]
    [Arguments(RevealAll, "Hello\\u200B World12\u2002\u00A0!\u2003\u202E\n\u2009\r\t\btest", "Hello\\u200B World12\\u2002\\u00A0!\\u2003\\u202E\\u000A\\u2009\\u000D\\u0009\\u0008test")]
    [Arguments(RevealNonStandard, "Hello\u200B World12\u2002\u00A0!\u2003\u202E\n\u2009\r\t\btest", "Hello\\u200B World12\\u2002\\u00A0!\\u2003\\u202E\n\\u2009\r\t\btest")]
    [Arguments(RemoveAll, "Hello\u200B World12\u2002\u00A0!\u2003\u202E\n\u2009\r\t\btest", "Hello World12!test")]
    [Arguments(RemoveNonStandard, "Hello\u200B World12\u2002\u00A0!\u2003\u202E\n\u2009\r\t\btest", "Hello World12!\n\r\t\btest")]
    
    [Arguments(RevealAll, "Привіт\u200B Світ12\u2002\u00A0!\u2003\u202E\n\u2009\r\t\btest", "Привіт\\u200B Світ12\\u2002\\u00A0!\\u2003\\u202E\\u000A\\u2009\\u000D\\u0009\\u0008test")]
    [Arguments(RevealNonStandard, "Привіт\u200B Світ12\u2002\u00A0!\u2003\u202E\n\u2009\r\t\btest", "Привіт\\u200B Світ12\\u2002\\u00A0!\\u2003\\u202E\n\\u2009\r\t\btest")]
    [Arguments(RemoveAll, "Привіт\u200B Світ12\u2002\u00A0!\u2003\u202E\n\u2009\r\t\btest", "Привіт Світ12!test")]
    [Arguments(RemoveNonStandard, "Привіт\u200B Світ12\u2002\u00A0!\u2003\u202E\n\u2009\r\t\btest", "Привіт Світ12!\n\r\t\btest")]
    public async Task ShouldRevealCharacters_BasedOnViewType(CharacterViewerTool.ViewType viewType, string inputText, string expectedText)
    {
        var args = new CharacterViewerTool.Args(
            Text: inputText,
            ViewType: viewType
        );

        var result = await new CharacterViewerTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Text).IsEqualTo(expectedText);
    }
    
    [Test]
    [Arguments("Hello12\u200B \u2002\u00A0!\u2003\n\u2009\r\t\b", new[]{"\\u200B", "\u2002", "\n", "\\r", "U+2003", "u2029"}, "Hello12​  \\u00A0! \n\\u2009\r\t\b")]
    public async Task ShouldTreatAsStandard_WhenCharactersOrHexCodeSpecified(string inputText, string[] characters, string expectedText)
    {
        var args = new CharacterViewerTool.Args(
            Text: inputText,
            ViewType: RevealNonStandard,
            TreatAsStandardHexCodes:characters
        );

        var result = await new CharacterViewerTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Text).IsEqualTo(expectedText);
    }
    
    [Test]
    [Arguments("H1 \n\r\u2002")]
    [Arguments("Привіт")]
    public async Task ShouldProduceCorrectCharInfo(string inputText)
    {
        var args = new CharacterViewerTool.Args(
            Text: inputText,
            ViewType: RevealAll,
            IncludeCharInfo: true
        );

        var result = await new CharacterViewerTool().RunAsync(args, CancellationToken.None);

        var codes = result.CharInfos.ToDictionary(it => it.Char, it => it.HexCode);
        foreach (var ch in inputText)
        {
            var hexCode = $"{(int)ch:X4}";
            await Assert.That(codes).ContainsKey(ch);
            await Assert.That(codes.Values).Contains(hexCode);
        }
    }
}
