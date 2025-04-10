using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class XmlFormatterToolTests
{
    private const string TestXml = "<Root xmlns1=\"test\"><section2><Key1>Value1</Key1><NestedSection><NestedKey1 attr=\"attrValue\">1</NestedKey1></NestedSection><Empty/><DefaultBool>false</DefaultBool><DefaultNumber>0</DefaultNumber><Array1><Item>value2</Item><Item>value1</Item></Array1><Array2><Item><key2>value2</key2><key1>value1</key1></Item><Item><key>value2</key></Item></Array2></section2><Section1><KeyB>1.2</KeyB><KeyA>true</KeyA></Section1></Root>";

    [Theory]
    [InlineData(null, ErrorCode.InputNotValid)]
    [InlineData("", ErrorCode.InputNotValid)]
    [InlineData("  ", ErrorCode.InputNotValid)]
    [InlineData("{\"key\":}", ErrorCode.InputNotValid)]
    public async Task Should_Format_Xml_Should_Fail(string? xml, ErrorCode errorCode)
    {
        var result = await new XmlFormatterTool().RunAsync(new XmlFormatterTool.Args
        {
            Xml = xml!
        }, CancellationToken.None);

        Assert.Equal(errorCode, result.ErrorCodes.First());
    }
    
    [Fact]
    public async Task Should_Format_Xml()
    {
        var result = await new XmlFormatterTool().RunAsync(new XmlFormatterTool.Args
        {
            Xml = TestXml
        }, CancellationToken.None);

        await Verify(result.Xml).UseSnapshotFolder();
    }
    
    [Fact]
    public async Task Should_Format_Xml_With_Right_Indent()
    {
        var result = await new XmlFormatterTool().RunAsync(new XmlFormatterTool.Args
        {
            Xml = TestXml,
            IndentSize = 3
        }, CancellationToken.None);

        await Verify(result.Xml).UseSnapshotFolder();
    }
    
    [Fact]
    public async Task Should_Format_Xml_And_Sort_Keys_Asc()
    {
        var result = await new XmlFormatterTool().RunAsync(new XmlFormatterTool.Args
        {
            Xml = TestXml,
            SortKeys = XmlFormatterTool.SortDirection.Ascending
        }, CancellationToken.None);

        await Verify(result.Xml).UseSnapshotFolder();
    }
    
    [Fact]
    public async Task Should_Format_Xml_And_Sort_Keys_Desc()
    {
        var result = await new XmlFormatterTool().RunAsync(new XmlFormatterTool.Args
        {
            Xml = TestXml,
            SortKeys = XmlFormatterTool.SortDirection.Descending
        }, CancellationToken.None);

        await Verify(result.Xml).UseSnapshotFolder();
    }
    
    [Fact]
    public async Task Should_Format_Xml_And_Exclude_Nulls()
    {
        var result = await new XmlFormatterTool().RunAsync(new XmlFormatterTool.Args
        {
            Xml = TestXml,
            ExcludeEmpty = true
        }, CancellationToken.None);

        await Verify(result.Xml).UseSnapshotFolder();
    }
    
    [Fact]
    public async Task Should_Format_Xml_As_Compact()
    {
        var result = await new XmlFormatterTool().RunAsync(new XmlFormatterTool.Args
        {
            Xml = TestXml,
            Compact = true
        }, CancellationToken.None);

        await Verify(result.Xml).UseSnapshotFolder();
    }
    
    [Fact]
    public async Task Should_Format_Xml_With_Uppercase()
    {
        var result = await new XmlFormatterTool().RunAsync(new XmlFormatterTool.Args
        {
            Xml = TestXml,
            KeyFormat = XmlFormatterTool.TextCase.UpperCase
        }, CancellationToken.None);

        await Verify(result.Xml).UseSnapshotFolder();
    }
    
    [Fact]
    public async Task Should_Format_Xml_With_Lowercase()
    {
        var result = await new XmlFormatterTool().RunAsync(new XmlFormatterTool.Args
        {
            Xml = TestXml,
            KeyFormat = XmlFormatterTool.TextCase.LowerCase
        }, CancellationToken.None);

        await Verify(result.Xml).UseSnapshotFolder();
    }
    
    [Fact]
    public async Task Should_Format_Xml_With_Pascalcase()
    {
        var result = await new XmlFormatterTool().RunAsync(new XmlFormatterTool.Args
        {
            Xml = TestXml,
            KeyFormat = XmlFormatterTool.TextCase.PascalCase
        }, CancellationToken.None);

        await Verify(result.Xml).UseSnapshotFolder();
    }
    
    [Fact]
    public async Task Should_Format_Xml_With_Camelcase()
    {
        var result = await new XmlFormatterTool().RunAsync(new XmlFormatterTool.Args
        {
            Xml = TestXml,
            KeyFormat = XmlFormatterTool.TextCase.CamelCase
        }, CancellationToken.None);

        await Verify(result.Xml).UseSnapshotFolder();
    }
}