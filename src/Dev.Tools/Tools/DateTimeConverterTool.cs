using System.Globalization;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "date-convert",
    Aliases = [],
    Keywords = [Keyword.Generate, Keyword.Text, Keyword.String, Keyword.Hash],
    Categories = [Category.Text, Category.Crypto, Category.Security],
    ErrorCodes = [ErrorCode.Unknown, ErrorCode.TextEmpty]
)]
public sealed class DateTimeConverterTool : ToolBase<DateTimeConverterTool.Args, DateTimeConverterTool.Result>
{
    protected override Result Execute(Args args)
    {
        var date = ConvertStringToDate(args.Date, args.From);
        if (date is null)
        {
            return Failed(ErrorCode.WrongFormat);
        }
        
        var result = ConvertDateToString(date.Value, args.To);
        if (result is null)
        {
            return Failed(ErrorCode.WrongFormat);
        }
        
        return new Result(result);
    }

    private static DateTime? ConvertStringToDate(string? input, DateFormatType formatType)
    {
        if (string.IsNullOrEmpty(input))
        {
            return DateTime.Now;
        }

        try
        {
            DateTime result;
            return formatType switch
            {
                DateFormatType.Iso8601 or DateFormatType.JsDateTime => DateTime.TryParse(input, null, DateTimeStyles.RoundtripKind, out result)
                    ? result
                    : null,
                DateFormatType.Iso9075 => DateTime.TryParseExact(input, "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
                    ? result
                    : null,
                DateFormatType.Rfc3339 => DateTime.TryParse(input, null, DateTimeStyles.RoundtripKind, out result)
                    ? result
                    : null,
                DateFormatType.Rfc7231 => DateTime.TryParseExact(input, "ddd, dd MMM yyyy HH:mm:ss GMT",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
                    ? result
                    : null,
                DateFormatType.Unix => long.TryParse(input, out long unixSeconds)
                    ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds).DateTime
                    : null,
                DateFormatType.Timestamp => DateTime.TryParseExact(input, "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
                    ? result
                    : null,
                DateFormatType.Iso8601Utc => DateTime.TryParse(input, null,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result)
                    ? result
                    : null,
                DateFormatType.MongoObjectId => new DateTime(1970, 1, 1)
                    .AddSeconds(Convert.ToInt32(input.Substring(0, 8), 16))
                    .ToLocalTime(),
                DateFormatType.ExcelDateTime => double.TryParse(input, out double oaDate)
                    ? DateTime.FromOADate(oaDate)
                    : null,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
    
    public static string? ConvertDateToString(DateTime date, DateFormatType formatType)
    {
        return formatType switch
        {
            DateFormatType.Iso8601 => date.ToString("o", CultureInfo.InvariantCulture),
            DateFormatType.Iso9075 => date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateFormatType.Rfc3339 => date.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture),
            DateFormatType.Rfc7231 => date.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture),
            DateFormatType.Unix => ((DateTimeOffset)date).ToUnixTimeSeconds().ToString(),
            DateFormatType.Timestamp => ((DateTimeOffset)date).ToUnixTimeMilliseconds().ToString(),
            DateFormatType.Iso8601Utc => date.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            DateFormatType.MongoObjectId => $"{(int)(date.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds:X8}0000000000000000",
            DateFormatType.ExcelDateTime => date.ToOADate().ToString(CultureInfo.InvariantCulture),
            DateFormatType.JsDateTime => date.ToString("o", CultureInfo.InvariantCulture),
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
    
    public record Args : ToolArgs
    {
        public string? Date { get; set; }
        public DateFormatType From { get; set; }
        public DateFormatType To { get; set; }
    }

    public record Result(string Time) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}