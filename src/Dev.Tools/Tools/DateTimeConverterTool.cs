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
        if (string.IsNullOrEmpty(args.Date))
        {
            return Failed(ErrorCode.Unknown);
        }


        DateTime? date = args.From switch
        {
            DateFormatType.Iso8601 => DateTime.TryParse(args.Date, null, styles: DateTimeStyles.RoundtripKind, out var value) ? value: null,
            DateFormatType.Iso9075 => DateTime.TryParse(args.Date, out var value) ? value : null,
            DateFormatType.Rfc3339 => DateTime.TryParse(args.Date, out var value) ? value : null,
            DateFormatType.Rfc7231 => DateTime.TryParse(args.Date, out var value) ? value : null,
            DateFormatType.Unix => throw new NotImplementedException(),
            DateFormatType.Timestamp => throw new NotImplementedException(),
            DateFormatType.UTC => throw new NotImplementedException(),
            DateFormatType.MongoObjectID => throw new NotImplementedException(),
            DateFormatType.ExcelDdateTime => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };


        

        return base.Execute(args);
    }

    public enum DateFormatType
    {
        Iso8601,
        Iso9075,
        Rfc3339,
        Rfc7231,
        Unix,
        Timestamp,
        UTC,
        MongoObjectID,
        ExcelDdateTime
    }


    public record Args(
        string Date,
        DateFormatType From,
        DateFormatType To
    ) : ToolArgs;

    public record Result(string Text) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}
