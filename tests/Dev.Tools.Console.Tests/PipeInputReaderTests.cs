using System.Text;
using Dev.Tools.Console.Core;

namespace Dev.Tools.Console.Tests;

public class PipeInputReaderTests
{
    private static PipeInputReader Piped(string stdin)
        => new(() => true, () => new MemoryStream(Encoding.UTF8.GetBytes(stdin)));

    // Stdin that fails the test if it is opened: reading it when it should not be is what hung commands.
    private static PipeInputReader Untouchable(bool redirected)
        => new(() => redirected, () => throw new InvalidOperationException("stdin was read"));

    [Test]
    public async Task GivenArgument_ShouldWin_WithoutReadingStdin()
    {
        await Assert.That(Untouchable(redirected: true).ReadText("given")).IsEqualTo("given");
    }

    [Test]
    public async Task GivenArgument_ShouldWinForContentToo_WithoutReadingStdin()
    {
        var content = Untouchable(redirected: true).ReadContent("given");

        await Assert.That(await content.ReadAllTextAsync(CancellationToken.None)).IsEqualTo("given");
    }

    [Test]
    public async Task NoArgument_ShouldReadPipedText_WithoutItsTrailingLineBreak()
    {
        await Assert.That(Piped("piped\r\n").ReadText(null)).IsEqualTo("piped");
    }

    [Test]
    public async Task NoArgument_ShouldStreamPipedContent_AsItIs()
    {
        var content = Piped("hello\n").ReadContent(null);

        await Assert.That(await content.ReadAllBytesAsync(CancellationToken.None)).IsEquivalentTo("hello\n"u8.ToArray());
    }

    [Test]
    public async Task BlankArgument_ShouldReadStdin()
    {
        await Assert.That(Piped("piped").ReadText("  ")).IsEqualTo("piped");
    }

    [Test]
    public async Task NothingPiped_ShouldLeaveTheArgumentEmpty()
    {
        var reader = Untouchable(redirected: false);

        await Assert.That(reader.ReadText(null)).IsEqualTo(string.Empty);
        await Assert.That(await reader.ReadContent(null).ReadAllBytesAsync(CancellationToken.None)).IsEmpty();
    }
}
