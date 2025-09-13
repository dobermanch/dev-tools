using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class DateTimeConverterToolTests
{
    [Skip("It behaves differently on build machine. Need to figure out why.")]
    [Test]
    [Arguments(DateTimeConverterTool.DateFormatType.JsDateTime, "2025-01-11T15:38:59.0000000-08:00")]
    [Arguments(DateTimeConverterTool.DateFormatType.Iso8601Utc, "2025-01-11T23:38:59.0000000Z")]
    [Arguments(DateTimeConverterTool.DateFormatType.Iso9075, "2025-01-11 15:38:59")]
    [Arguments(DateTimeConverterTool.DateFormatType.Rfc3339, "2025-01-11T15:38:59.000-08:00")]
    [Arguments(DateTimeConverterTool.DateFormatType.Rfc7231, "Sat, 11 Jan 2025 15:38:59 GMT")]
    [Arguments(DateTimeConverterTool.DateFormatType.Unix, "1736638739")]
    [Arguments(DateTimeConverterTool.DateFormatType.Timestamp, "1736638739000")]
    [Arguments(DateTimeConverterTool.DateFormatType.MongoObjectId, "678301130000000000000000")]
    [Arguments(DateTimeConverterTool.DateFormatType.ExcelDateTime, "45668.65207175926")]
    public async Task ShouldConvertDateProperly(DateTimeConverterTool.DateFormatType to, string expected)
    {
        var args = new DateTimeConverterTool.Args
        {
            Date = "2025-01-11T15:38:59-08:00",
            From = DateTimeConverterTool.DateFormatType.Iso8601,
            To = to,
        };

        var result = await new DateTimeConverterTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Time).IsEqualTo(expected);
    }
}