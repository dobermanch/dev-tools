using System.Text;

namespace Dev.Tools.Tests;

public class ToolContentTests
{
    [Test]
    public async Task Text_ShouldReadAsUtf8Bytes()
    {
        var bytes = await ToolContent.FromText("Тест").ReadAllBytesAsync(CancellationToken.None);

        await Assert.That(bytes).IsEquivalentTo(Encoding.UTF8.GetBytes("Тест"));
    }

    [Test]
    public async Task Bytes_ShouldReadAsText_HonouringAByteOrderMark()
    {
        var content = ToolContent.FromBytes(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'h', (byte)'i' });

        await Assert.That(await content.ReadAllTextAsync(CancellationToken.None)).IsEqualTo("hi");
    }

    [Test]
    public async Task InvalidUtf8_ShouldReadAsReplacementCharacters_RatherThanFail()
    {
        var content = ToolContent.FromBytes(new byte[] { (byte)'h', 0xFF });

        await Assert.That(await content.ReadAllTextAsync(CancellationToken.None)).IsEqualTo("h�");
    }

    [Test]
    public async Task Stream_ShouldBeOpenedEachTimeItIsRead()
    {
        var opened = 0;
        var content = ToolContent.FromStream(() =>
        {
            opened++;
            return new MemoryStream("abc"u8.ToArray());
        });

        await Assert.That(await content.ReadAllTextAsync(CancellationToken.None)).IsEqualTo("abc");
        await Assert.That(await content.ReadAllBytesAsync(CancellationToken.None)).IsEquivalentTo("abc"u8.ToArray());
        await Assert.That(opened).IsEqualTo(2);
    }

    [Test]
    public async Task NoString_ShouldConvertToEmptyContent()
    {
        ToolContent content = (string?)null;

        await Assert.That(await content.ReadAllBytesAsync(CancellationToken.None)).IsEmpty();
    }
}
