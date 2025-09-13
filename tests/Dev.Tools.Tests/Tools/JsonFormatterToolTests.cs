using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class JsonFormatterToolTests
{
    private const string TestJson = "{\"section2\":{\"Key1\":\"Value1\",\"NestedSection\":{\"NestedKey1\":1},\"Empty\":null,\"DefaultBool\":false,\"DefaultNumber\":0,\"Array1\":[\"value2\",\"value1\"],\"Array2\":[{\"key2\":\"value2\",\"key1\":\"value1\"},{\"key\":\"value2\"}]},\"Section1\":{\"KeyB\":1.2,\"KeyA\":true}}";

    [Test]
    [Arguments(null, ErrorCode.InputNotValid)]
    [Arguments("", ErrorCode.InputNotValid)]
    [Arguments("  ", ErrorCode.InputNotValid)]
    [Arguments("{\"key\":}", ErrorCode.InputNotValid)]
    public async Task Should_Format_Json_Should_Fail(string? json, ErrorCode errorCode)
    {
        var result = await new JsonFormatterTool().RunAsync(new JsonFormatterTool.Args
        {
            Json = json!
        }, CancellationToken.None);

        await Assert.That(result.ErrorCodes.First()).IsEqualTo(errorCode);
    }
    
    [Test]
    public async Task Should_Format_Json()
    {
        var result = await new JsonFormatterTool().RunAsync(new JsonFormatterTool.Args
        {
            Json = TestJson
        }, CancellationToken.None);

        await Verify(result.Json).UseSnapshotFolder();
    }
    
    [Test]
    public async Task Should_Format_Json_With_Right_Indent()
    {
        var result = await new JsonFormatterTool().RunAsync(new JsonFormatterTool.Args
        {
            Json = TestJson,
            IndentSize = 3
        }, CancellationToken.None);

        await Verify(result.Json).UseSnapshotFolder();
    }
    
    [Test]
    public async Task Should_Format_Json_And_Sort_Keys_Asc()
    {
        var result = await new JsonFormatterTool().RunAsync(new JsonFormatterTool.Args
        {
            Json = TestJson,
            SortKeys = JsonFormatterTool.SortDirection.Ascending
        }, CancellationToken.None);

        await Verify(result.Json).UseSnapshotFolder();
    }
    
    [Test]
    public async Task Should_Format_Json_And_Sort_Keys_Desc()
    {
        var result = await new JsonFormatterTool().RunAsync(new JsonFormatterTool.Args
        {
            Json = TestJson,
            SortKeys = JsonFormatterTool.SortDirection.Descending
        }, CancellationToken.None);

        await Verify(result.Json).UseSnapshotFolder();
    }
    
    [Test]
    public async Task Should_Format_Json_And_Exclude_Nulls()
    {
        var result = await new JsonFormatterTool().RunAsync(new JsonFormatterTool.Args
        {
            Json = TestJson,
            ExcludeEmpty = true
        }, CancellationToken.None);

        await Verify(result.Json).UseSnapshotFolder();
    }
    
    [Test]
    public async Task Should_Format_Json_As_Compact()
    {
        var result = await new JsonFormatterTool().RunAsync(new JsonFormatterTool.Args
        {
            Json = TestJson,
            Compact = true
        }, CancellationToken.None);

        await Verify(result.Json).UseSnapshotFolder();
    }
    
    [Test]
    public async Task Should_Format_Json_With_Uppercase()
    {
        var result = await new JsonFormatterTool().RunAsync(new JsonFormatterTool.Args
        {
            Json = TestJson,
            KeyFormat = JsonFormatterTool.TextCase.UpperCase
        }, CancellationToken.None);

        await Verify(result.Json).UseSnapshotFolder();
    }
    
    [Test]
    public async Task Should_Format_Json_With_Lowercase()
    {
        var result = await new JsonFormatterTool().RunAsync(new JsonFormatterTool.Args
        {
            Json = TestJson,
            KeyFormat = JsonFormatterTool.TextCase.LowerCase
        }, CancellationToken.None);

        await Verify(result.Json).UseSnapshotFolder();
    }
    
    [Test]
    public async Task Should_Format_Json_With_Pascalcase()
    {
        var result = await new JsonFormatterTool().RunAsync(new JsonFormatterTool.Args
        {
            Json = TestJson,
            KeyFormat = JsonFormatterTool.TextCase.PascalCase
        }, CancellationToken.None);

        await Verify(result.Json).UseSnapshotFolder();
    }
    
    [Test]
    public async Task Should_Format_Json_With_Camelcase()
    {
        var result = await new JsonFormatterTool().RunAsync(new JsonFormatterTool.Args
        {
            Json = TestJson,
            KeyFormat = JsonFormatterTool.TextCase.CamelCase
        }, CancellationToken.None);

        await Verify(result.Json).UseSnapshotFolder();
    }
}