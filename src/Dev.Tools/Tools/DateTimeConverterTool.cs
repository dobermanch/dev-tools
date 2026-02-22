using System.Globalization;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "date-convert",
    Aliases = [],
    Keywords = [Keyword.Generate, Keyword.Text, Keyword.String, Keyword.Hash],
    Categories = [Category.Text, Category.Crypto, Category.Security]
)]
public sealed class DateTimeConverterTool : ToolBase<DateTimeConverterTool.Args, DateTimeConverterTool.Result>
{
    protected override Result Execute(Args args)
    {
        var date = ConvertStringToDate(args.Date, args.From);
        if (date is null)
        {
            throw new ToolException(ErrorCode.WrongFormat);
        }

        var result = ConvertDateToString(date.Value, args.To);
        if (result is null)
        {
            throw new ToolException(ErrorCode.WrongFormat);
        }

        return new Result(result);
    }

    private static DateTimeOffset? ConvertStringToDate(string? input, DateFormatType formatType)
    {
        if (string.IsNullOrEmpty(input))
        {
            return DateTimeOffset.Now;
        }

        try
        {
            return formatType switch
            {
                DateFormatType.Iso8601 or DateFormatType.JsDateTime or DateFormatType.Rfc3339 =>
                    DateTimeOffset.TryParse(input, null, DateTimeStyles.RoundtripKind, out var r1) ? r1 : null,
                DateFormatType.Iso8601Utc =>
                    DateTimeOffset.TryParse(input, null,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var r2) ? r2 : null,
                DateFormatType.Iso9075 =>
                    DateTimeOffset.TryParseExact(input, "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var r3) ? r3 : null,
                DateFormatType.Rfc7231 =>
                    DateTimeOffset.TryParseExact(input, "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var r4) ? r4 : null,
                DateFormatType.Unix =>
                    long.TryParse(input, out long unixSeconds)
                        ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds)
                        : null,
                DateFormatType.Timestamp =>
                    DateTimeOffset.TryParseExact(input, "yyyyMMddHHmmss",
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var r5) ? r5 : null,
                DateFormatType.MongoObjectId =>
                    DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt32(input.Substring(0, 8), 16)),
                DateFormatType.ExcelDateTime =>
                    double.TryParse(input, CultureInfo.InvariantCulture, out double oaDate)
                        ? new DateTimeOffset(DateTime.FromOADate(oaDate),
                            TimeZoneInfo.Local.GetUtcOffset(DateTime.FromOADate(oaDate)))
                        : null,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    public static string? ConvertDateToString(DateTimeOffset date, DateFormatType formatType)
    {
        return formatType switch
        {
            DateFormatType.Iso8601       => date.ToString("o", CultureInfo.InvariantCulture),
            DateFormatType.JsDateTime    => date.ToString("o", CultureInfo.InvariantCulture),
            DateFormatType.Iso9075       => date.DateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateFormatType.Rfc3339       => date.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture),
            DateFormatType.Rfc7231       => date.DateTime.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture),
            DateFormatType.Unix          => date.ToUnixTimeSeconds().ToString(),
            DateFormatType.Timestamp     => date.ToUnixTimeMilliseconds().ToString(),
            DateFormatType.Iso8601Utc    => date.UtcDateTime.ToString("o", CultureInfo.InvariantCulture),
            DateFormatType.MongoObjectId =>
                $"{(int)(date.UtcDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds:X8}0000000000000000",
            DateFormatType.ExcelDateTime => date.DateTime.ToOADate().ToString(CultureInfo.InvariantCulture),
            _ => null
        };
    }

    public enum DateFormatType
    {
        Iso8601,
        Iso8601Utc,
        Iso9075,
        Rfc3339,
        Rfc7231,
        Unix,
        Timestamp,
        MongoObjectId,
        ExcelDateTime,
        JsDateTime
    }

    public sealed record Args(
        [property: PipeInput] string Date,
        DateFormatType From = DateFormatType.Iso8601,
        DateFormatType To = DateFormatType.Iso8601
    );

    public sealed record Result([property: PipeOutput] string Time) : ToolResult
    {
        public Result() : this(string.Empty)
        {
        }
    }
}