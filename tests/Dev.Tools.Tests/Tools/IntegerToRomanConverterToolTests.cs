using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class IntegerToRomanConverterToolTests
{
    [Test]
    [Arguments("1", "I")]
    [Arguments("42", "XLII")]
    [Arguments("3999", "MMMCMXCIX")]
    public async Task ShouldEncodeIntegerToRomanNumber(string number, string expectedResult)
    {
        var args = new IntegerToRomanConverterTool.Args(number, IntegerToRomanConverterTool.TranscodingType.Encode);

        var result = await new IntegerToRomanConverterTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Number).IsEqualTo(expectedResult);
    }
    
    [Test]
    [Arguments("I", "1")]
    [Arguments("XLII", "42")]
    [Arguments("MMMCMXCIX", "3999")]
    public async Task ShouldDecodeRomanNumberToInteger(string number, string expectedResult)
    {
        var args = new IntegerToRomanConverterTool.Args(number, IntegerToRomanConverterTool.TranscodingType.Decode);

        var result = await new IntegerToRomanConverterTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Number).IsEqualTo(expectedResult);
    }

    [Test]
    public async Task ShouldReturnEmptyErrorCode_WhenNumberIsNotValid()
    {
        var args = new IntegerToRomanConverterTool.Args("", IntegerToRomanConverterTool.TranscodingType.Encode);

        var result = await new IntegerToRomanConverterTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.TextEmpty);
    }
    
    [Test]
    [Arguments("12d", IntegerToRomanConverterTool.TranscodingType.Encode)]
    [Arguments("0", IntegerToRomanConverterTool.TranscodingType.Encode)]
    [Arguments("4000", IntegerToRomanConverterTool.TranscodingType.Encode)]
    [Arguments("YH", IntegerToRomanConverterTool.TranscodingType.Decode)]
    public async Task ShouldReturnWrongFormatErrorCode_WhenNumberIsNotValid(string input, IntegerToRomanConverterTool.TranscodingType transcoding)
    {
        var args = new IntegerToRomanConverterTool.Args(input, transcoding);

        var result = await new IntegerToRomanConverterTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.WrongFormat);
    }
}