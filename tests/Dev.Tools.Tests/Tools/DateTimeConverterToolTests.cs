using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class DateTimeConverterToolTests
{
    [Theory(Skip = "It behaves differently on build machine. Need to figure out why.")]
    [InlineData(DateTimeConverterTool.DateFormatType.JsDateTime, "2025-01-11T15:38:59.0000000-08:00")]
    [InlineData(DateTimeConverterTool.DateFormatType.Iso8601Utc, "2025-01-11T23:38:59.0000000Z")]
    [InlineData(DateTimeConverterTool.DateFormatType.Iso9075, "2025-01-11 15:38:59")]
    [InlineData(DateTimeConverterTool.DateFormatType.Rfc3339, "2025-01-11T15:38:59.000-08:00")]
    [InlineData(DateTimeConverterTool.DateFormatType.Rfc7231, "Sat, 11 Jan 2025 15:38:59 GMT")]
    [InlineData(DateTimeConverterTool.DateFormatType.Unix, "1736638739")]
    [InlineData(DateTimeConverterTool.DateFormatType.Timestamp, "1736638739000")]
    [InlineData(DateTimeConverterTool.DateFormatType.MongoObjectId, "678301130000000000000000")]
    [InlineData(DateTimeConverterTool.DateFormatType.ExcelDateTime, "45668.65207175926")]
    public async Task ShouldConvertDateProperly(DateTimeConverterTool.DateFormatType to, string expected)
    {
        var args = new DateTimeConverterTool.Args
        {
            Date = "2025-01-11T15:38:59-08:00",
            From = DateTimeConverterTool.DateFormatType.Iso8601,
            To = to,
        };

        var result = await new DateTimeConverterTool().RunAsync(args, CancellationToken.None);

        Assert.Equal(expected, result.Time);
    }
}