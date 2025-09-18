using Dev.Tools.Tools;
using static Dev.Tools.Tools.IntegerBaseConverterTool.BaseType;

namespace Dev.Tools.Tests.Tools;

public class IntegerBaseConverterToolTests
{
    [Test]
    [Arguments("1234", IntegerBaseConverterTool.BaseType.Decimal, Binary, "10011010010")]
    [Arguments("1234", IntegerBaseConverterTool.BaseType.Decimal, Octal, "2322")]
    [Arguments("1234", IntegerBaseConverterTool.BaseType.Decimal, IntegerBaseConverterTool.BaseType.Decimal, "1234")]
    [Arguments("1234", IntegerBaseConverterTool.BaseType.Decimal, Hexadecimal, "4d2")]
    [Arguments("1234", IntegerBaseConverterTool.BaseType.Decimal, Base64, "0gQAAA==")]
    [Arguments("1234", IntegerBaseConverterTool.BaseType.Decimal, 40, "ei")]
    [Arguments("10011010010", Binary, IntegerBaseConverterTool.BaseType.Decimal, "1234")]
    [Arguments("2322", Octal, IntegerBaseConverterTool.BaseType.Decimal, "1234")]
    [Arguments("4d2", Hexadecimal, IntegerBaseConverterTool.BaseType.Decimal, "1234")]
    [Arguments("0gQAAA==", Base64, IntegerBaseConverterTool.BaseType.Decimal, "1234")]
    [Arguments("ei", 40, IntegerBaseConverterTool.BaseType.Decimal, "1234")]
    public async Task ShouldDecodeString_FromBase64(string inputValue, IntegerBaseConverterTool.BaseType inputBase, IntegerBaseConverterTool.BaseType targetBase, string expectedValue)
    {
        var args = new IntegerBaseConverterTool.Args
        (
            InputValue: inputValue,
            InputBase: inputBase,
            TargetBase: targetBase
        );

        var result = await new IntegerBaseConverterTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Data).IsEqualTo(expectedValue);
        await Assert.That(result.Base).IsEqualTo(targetBase);
    }

    [Test]
    public async Task ShouldReturnInputNotValidErrorCode_WhenInputIsNotProvided()
    {
        var args = new IntegerBaseConverterTool.Args(
            null!,
            IntegerBaseConverterTool.BaseType.Decimal,
            IntegerBaseConverterTool.BaseType.Decimal
        );

        var result = await new IntegerBaseConverterTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.InputNotValid);
    }
    
    [Test]
    [Arguments(0, IntegerBaseConverterTool.BaseType.Decimal)]
    [Arguments(65, IntegerBaseConverterTool.BaseType.Decimal)]
    [Arguments(IntegerBaseConverterTool.BaseType.Decimal, 0)]
    [Arguments(IntegerBaseConverterTool.BaseType.Decimal, 65)]
    public async Task ShouldReturnInputWrongBase_WhenInputIsNotProvided(IntegerBaseConverterTool.BaseType inputBase, IntegerBaseConverterTool.BaseType targetBase)
    {
        var args = new IntegerBaseConverterTool.Args(
            "123",
            inputBase,
            targetBase
        );

        var result = await new IntegerBaseConverterTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.WrongBase);
    }
    
    [Test]
    public async Task ShouldReturnInputInputNotValid_WhenCustomInputIsWrong()
    {
        var args = new IntegerBaseConverterTool.Args
        (
            InputValue: "78",
            InputBase: (IntegerBaseConverterTool.BaseType)40,
            TargetBase: Base64
        );

        var result = await new IntegerBaseConverterTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.InputNotValid);
    }
}
