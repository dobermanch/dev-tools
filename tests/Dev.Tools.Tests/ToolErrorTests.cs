using Dev.Tools.Tools;

namespace Dev.Tools.Tests;

public class ToolErrorTests
{
    [Test]
    public async Task MessageShouldBeTheEnglishAuthoredOnTheCode()
    {
        await Assert.That(new ToolError(ErrorCode.TextEmpty, "Text").Message).IsEqualTo("Text cannot be empty");
    }

    [Test]
    public async Task EveryCodeShouldHaveAMessageOfItsOwn()
    {
        // A code without a [Description] falls back to its member name, which is not a message.
        var undescribed = Enum.GetValues<ErrorCode>().Where(it => new ToolError(it).Message == it.ToString()).ToArray();

        await Assert.That(undescribed).IsEmpty();
    }

    [Test]
    public async Task ARejectedInputShouldNameTheArgumentAtFault()
    {
        var result = await new BaseEncodingTool().RunAsync(new BaseEncodingTool.Args("!!!", BaseEncodingTool.TranscodingType.Decode), CancellationToken.None);

        await Assert.That(result.Errors)
            .IsEquivalentTo([new ToolError(ErrorCode.InputNotValid, nameof(BaseEncodingTool.Args.Text))]);
    }

    [Test]
    public async Task EveryMissingArgumentShouldBeReportedNotOnlyTheFirst()
    {
        var encrypted = await new EncryptionTool().RunAsync(new EncryptionTool.Args("", ""), CancellationToken.None);
        var decrypted = await new EncryptionTool().RunAsync(new EncryptionTool.Args("", "", EncryptionTool.EncryptionOperation.Decrypt), CancellationToken.None);

        await Assert.That(encrypted.Errors).IsEquivalentTo(
        [
            new ToolError(ErrorCode.TextEmpty, nameof(EncryptionTool.Args.Text)),
            new ToolError(ErrorCode.InputNotValid, nameof(EncryptionTool.Args.Key))
        ]);
        await Assert.That(decrypted.Errors).IsEquivalentTo(
        [
            new ToolError(ErrorCode.TextEmpty, nameof(EncryptionTool.Args.Text)),
            new ToolError(ErrorCode.InputNotValid, nameof(EncryptionTool.Args.Key))
        ]);
    }

    [Test]
    public async Task AnUnexpectedFailureShouldReportUnknownWithoutTheExceptionDetail()
    {
        var result = await new ThrowingTool().RunAsync(new object(), CancellationToken.None);

        await Assert.That(result.Errors).IsEquivalentTo([new ToolError(ErrorCode.Unknown)]);
        await Assert.That(result.Errors[0].Message).DoesNotContain("secret");
    }

    [Test]
    public async Task AToolExceptionShouldCarryAtLeastOneError()
    {
        await Assert.That(() => new ToolException([])).Throws<ArgumentException>();
    }

    private sealed class ThrowingTool : ToolBase<object, ToolResult>
    {
        protected override ToolResult Execute(object args)
            => throw new InvalidOperationException("connection string: secret");
    }
}
