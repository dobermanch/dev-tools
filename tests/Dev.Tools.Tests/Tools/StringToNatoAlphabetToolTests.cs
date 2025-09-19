using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class StringToNatoAlphabetToolTests
{
    [Test]
    [Arguments("hello #$ 12world", "Hotel Echo Lima Lima Oscar   # $   1 2 Whiskey Oscar Romeo Lima Delta")]
    [Arguments("привіт", "п р и в і т")]
    public async Task ShouldTranscodeToSafeUrlString(string input, string expectedResult)
    {
        var args = new StringToNatoAlphabetTool.Args(input);

        var result = await new StringToNatoAlphabetTool().RunAsync(args, CancellationToken.None);

        var expected = expectedResult.Split(' ',  StringSplitOptions.RemoveEmptyEntries);
        await Assert.That(result.Words).IsEquivalentTo(expected);
    }

    [Test]
    public async Task ShouldReturnEmptyErrorCode_WhenTextIsNotProvided()
    {
        var args = new StringToNatoAlphabetTool.Args("");

        var result = await new StringToNatoAlphabetTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.TextEmpty);
    }
}