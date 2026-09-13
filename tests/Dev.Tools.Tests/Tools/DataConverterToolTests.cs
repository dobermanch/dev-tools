using System.Text;
using System.Text.Json.Nodes;
using Dev.Tools.Tools;
using From = Dev.Tools.Tools.DataConverterTool.SourceFormat;
using To = Dev.Tools.Tools.DataConverterTool.TargetFormat;

namespace Dev.Tools.Tests.Tools;

public class DataConverterToolTests
{
    private static Task<DataConverterTool.Result> ConvertAsync(string text, From from, To to)
        => new DataConverterTool().RunAsync(new DataConverterTool.Args(text, from, to), CancellationToken.None);

    // Compared as compact JSON, so the test is about the data - key order included - and not the layout.
    private static async Task AssertSameJson(DataConverterTool.Result result, string expectedJson)
    {
        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(JsonNode.Parse(result.Text)?.ToJsonString())
            .IsEqualTo(JsonNode.Parse(expectedJson)?.ToJsonString());
    }

    private static async Task AssertError(DataConverterTool.Result result, ErrorCode code, string target)
    {
        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.Errors[0].Code).IsEqualTo(code);
        await Assert.That(result.Errors[0].Target).IsEqualTo(target);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task ShouldReturnTextEmpty_WhenThereIsNoText(string text)
    {
        var result = await ConvertAsync(text, From.Json, To.Yaml);

        await AssertError(result, ErrorCode.TextEmpty, nameof(DataConverterTool.Args.Text));
    }

    [Test]
    [Arguments("{\"a\": 1,}", From.Json)]
    [Arguments("{\"a\": 1, \"a\": 2}", From.Json)]
    [Arguments("a: [1, 2", From.Yaml)]
    [Arguments("a: 1\na: 2", From.Yaml)]
    [Arguments("a: 1\n---\nb: 2", From.Yaml)]
    [Arguments("a = ", From.Toml)]
    [Arguments("a = 1\na = 2", From.Toml)]
    [Arguments("<a><b></a>", From.Xml)]
    public async Task ShouldReturnInputNotValid_WhenTheTextIsNotValidInItsFormat(string text, From from)
    {
        var result = await ConvertAsync(text, from, To.Json);

        await AssertError(result, ErrorCode.InputNotValid, nameof(DataConverterTool.Args.Text));
    }

    [Test]
    public async Task ShouldReformat_WhenConvertingAFormatToItself()
    {
        var result = await ConvertAsync("{\"a\":[1,2]}", From.Json, To.Json);

        await Assert.That(result.Text).IsEqualTo("{\n  \"a\": [\n    1,\n    2\n  ]\n}");
    }

    [Test]
    public async Task ShouldTypeYamlScalarsByTheCoreSchema()
    {
        const string yaml =
            """
            string: hello
            quoted: "true"
            int: 42
            negative: -7
            leading: 007
            octal: 0o17
            hex: 0x1F
            float: 1.5
            exponent: 1e3
            big: 123456789012345678901234567890
            bool: true
            yes: yes
            tilde: ~
            null: null
            empty:
            tagged: !!str 42
            inf: .inf
            """;

        var result = await ConvertAsync(yaml, From.Yaml, To.Json);

        await AssertSameJson(result,
            """
            {"string":"hello","quoted":"true","int":42,"negative":-7,"leading":7,"octal":15,"hex":31,
             "float":1.5,"exponent":1000,"big":123456789012345678901234567890,"bool":true,"yes":"yes",
             "tilde":null,"null":null,"empty":null,"tagged":"42","inf":".inf"}
            """);
    }

    [Test]
    public async Task ShouldExpandYamlAliases()
    {
        var result = await ConvertAsync("base: &base {x: 1, y: [a, b]}\ncopy: *base", From.Yaml, To.Json);

        await AssertSameJson(result, """{"base":{"x":1,"y":["a","b"]},"copy":{"x":1,"y":["a","b"]}}""");
    }

    [Test]
    public async Task ShouldReturnInputNotValid_WhenYamlAliasesExpandPastTheLimit()
    {
        // "Billion laughs": each level names the one before it ten times - a million strings from seven lines.
        var yaml = new StringBuilder("l0: &l0 [lol, lol, lol, lol, lol, lol, lol, lol, lol, lol]\n");
        for (var level = 1; level <= 6; level++)
        {
            var previous = string.Join(", ", Enumerable.Repeat($"*l{level - 1}", 10));
            yaml.Append($"l{level}: &l{level} [{previous}]\n");
        }

        var result = await ConvertAsync(yaml.ToString(), From.Yaml, To.Json);

        await AssertError(result, ErrorCode.InputNotValid, nameof(DataConverterTool.Args.Text));
    }

    // Every value that YAML would read as something else if written plain.
    private const string AwkwardJson =
        """
        {"true":"true","number":"42","float":"1.5","empty":"","null":"null","tilde":"~","yes":"yes",
         "colon":"a: b","hash":"# not a comment","dash":"- item","multiline":"one\ntwo\n","unicode":"Привіт, 👍",
         "values":[1,-2.5,true,false,null,123456789012345678901234567890],"nested":{"empty":{},"list":[]}}
        """;

    [Test]
    public async Task ShouldKeepEveryValue_WhenJsonGoesThroughYamlAndBack()
    {
        var yaml = await ConvertAsync(AwkwardJson, From.Json, To.Yaml);
        var json = await ConvertAsync(yaml.Text, From.Yaml, To.Json);

        await AssertSameJson(json, AwkwardJson);
    }

    [Test]
    public async Task ShouldReadToml_WithDatesAsTheirText()
    {
        const string toml =
            """
            title = "Example"
            count = 42
            ratio = 1.5
            enabled = true
            released = 1979-05-27T07:32:00-08:00
            day = 1979-05-27
            infinite = inf
            ports = [8000, 8001]
            point = { x = 1, y = 2 }

            [owner]
            name = "Tom"

            [[products]]
            name = "Hammer"

            [[products]]
            name = "Nail"
            """;

        var result = await ConvertAsync(toml, From.Toml, To.Json);

        await AssertSameJson(result,
            """
            {"title":"Example","count":42,"ratio":1.5,"enabled":true,"released":"1979-05-27T07:32:00-08:00",
             "day":"1979-05-27","infinite":"inf","ports":[8000,8001],"point":{"x":1,"y":2},
             "owner":{"name":"Tom"},"products":[{"name":"Hammer"},{"name":"Nail"}]}
            """);
    }

    [Test]
    public async Task ShouldKeepEveryValue_WhenJsonGoesThroughTomlAndBack()
    {
        // Plain keys first: TOML writes them before any [table], so a round trip puts them there.
        const string json =
            """
            {"name":"x \"quoted\"\nline","count":-3,"ratio":0.25,"on":false,"mixed":[1,"a",[true]],
             "key with space":"v","server":{"host":"localhost","ports":[80,443]},
             "products":[{"name":"Hammer","sku":1},{"name":"Nail","sku":2}]}
            """;

        var toml = await ConvertAsync(json, From.Json, To.Toml);
        var back = await ConvertAsync(toml.Text, From.Toml, To.Json);

        await AssertSameJson(back, json);
    }

    [Test]
    [Arguments("{\"a\": null}")]
    [Arguments("{\"a\": [1, null]}")]
    [Arguments("[1, 2]")]
    [Arguments("\"text\"")]
    [Arguments("{\"a\": 123456789012345678901234567890}")]
    public async Task ShouldReturnWrongFormat_WhenTomlCannotHoldTheData(string json)
    {
        var result = await ConvertAsync(json, From.Json, To.Toml);

        await AssertError(result, ErrorCode.WrongFormat, nameof(DataConverterTool.Args.To));
    }

    [Test]
    public async Task ShouldMapXmlToObjects()
    {
        const string xml =
            """
            <?xml version="1.0"?>
            <!-- dropped -->
            <order id="7">
              <item>a</item>
              <note>hi</note>
              <item>b</item>
              <empty/>
              <blank></blank>
              <mixed lang="en">text</mixed>
              <data><![CDATA[<raw> & more]]></data>
            </order>
            """;

        var result = await ConvertAsync(xml, From.Xml, To.Json);

        await AssertSameJson(result,
            """
            {"order":{"@id":"7","item":["a","b"],"note":"hi","empty":null,"blank":"",
             "mixed":{"@lang":"en","#text":"text"},"data":"<raw> & more"}}
            """);
    }

    [Test]
    public async Task ShouldWriteXml_WithAttributesRepeatedElementsAndEscaping()
    {
        const string json = """{"order":{"@id":"7","item":["a","b"],"note":"x < y & \"z\"","empty":null}}""";

        var result = await ConvertAsync(json, From.Json, To.Xml);

        await Assert.That(result.Text).IsEqualTo(
            """
            <order id="7">
              <item>a</item>
              <item>b</item>
              <note>x &lt; y &amp; "z"</note>
              <empty />
            </order>
            """);
    }

    [Test]
    [Arguments("{\"a\": 1, \"b\": true}", "<root>\n  <a>1</a>\n  <b>true</b>\n</root>")]
    [Arguments("[1, 2]", "<root>\n  <item>1</item>\n  <item>2</item>\n</root>")]
    [Arguments("{\"list\": [1, 2]}", "<root>\n  <list>1</list>\n  <list>2</list>\n</root>")]
    [Arguments("{\"first name\": \"Ann\"}", "<first_x0020_name>Ann</first_x0020_name>")]
    public async Task ShouldWrapInRoot_WhenTheDataHasNoSingleRootElement(string json, string expectedXml)
    {
        var result = await ConvertAsync(json, From.Json, To.Xml);

        await Assert.That(result.Text).IsEqualTo(expectedXml);
    }

    [Test]
    public async Task ShouldKeepEveryValue_WhenXmlGoesThroughJsonAndBack()
    {
        const string xml =
            """
            <config version="2">
              <name first="Ann">Ann &amp; Bob</name>
              <server>
                <port>80</port>
                <port>443</port>
              </server>
              <p>Hello <b>world</b></p>
              <none />
            </config>
            """;

        var json = await ConvertAsync(xml, From.Xml, To.Json);
        var back = await ConvertAsync(json.Text, From.Json, To.Xml);
        var again = await ConvertAsync(back.Text, From.Xml, To.Json);

        await AssertSameJson(again, json.Text);
    }

    [Test]
    public async Task ShouldReturnInputNotValid_WhenXmlHasADocumentTypeDefinition()
    {
        const string xml = """<!DOCTYPE x [<!ENTITY e "boom">]><x>&e;</x>""";

        var result = await ConvertAsync(xml, From.Xml, To.Json);

        await AssertError(result, ErrorCode.InputNotValid, nameof(DataConverterTool.Args.Text));
    }

    [Test]
    public async Task ShouldWriteCsv_WithEveryKeyAsAColumnAndRfc4180Quoting()
    {
        const string json =
            """
            [{"name":"Ann","note":"a, \"b\""},
             {"name":"Bob","age":30,"tags":["x"]},
             {"name":null}]
            """;

        var result = await ConvertAsync(json, From.Json, To.Csv);

        // Four quotes: the expected text holds three in a row.
        await Assert.That(result.Text).IsEqualTo(
            """"
            name,note,age,tags
            Ann,"a, ""b""",,
            Bob,,30,"[""x""]"
            ,,,
            """");
    }

    [Test]
    [Arguments("{\"a\": 1}")]
    [Arguments("[1, 2]")]
    [Arguments("[{\"a\": 1}, 2]")]
    public async Task ShouldReturnWrongFormat_WhenCsvIsNotFromAnArrayOfObjects(string json)
    {
        var result = await ConvertAsync(json, From.Json, To.Csv);

        await AssertError(result, ErrorCode.WrongFormat, nameof(DataConverterTool.Args.To));
    }

    [Test]
    public async Task ShouldWriteYaml_QuotingOnlyWhatWouldReadBackAsAnotherType()
    {
        const string json =
            """
            {"name":"Ann","true":"true","n":"42","empty":"","multi":"one\ntwo\n",
             "list":[1,"x",{"k":null}],"obj":{},"arr":[],"products":[{"sku":1}]}
            """;

        var result = await ConvertAsync(json, From.Json, To.Yaml);

        await Assert.That(result.Text).IsEqualTo(
            """
            name: Ann
            "true": "true"
            n: "42"
            empty: ""
            multi: |
              one
              two
            list:
            - 1
            - x
            - k: null
            obj: {}
            arr: []
            products:
            - sku: 1

            """);
    }

    [Test]
    public async Task ShouldWriteToml_WithTablesAndArraysOfTables()
    {
        const string json = """{"title":"x","server":{"host":"h","ports":[1,2]},"products":[{"sku":1},{"sku":2}]}""";

        var result = await ConvertAsync(json, From.Json, To.Toml);

        await Assert.That(result.Text).IsEqualTo(
            """
            title = "x"
            [server]
            host = "h"
            ports = [1, 2]
            [[products]]
            sku = 1

            [[products]]
            sku = 2
            """);
    }
}
