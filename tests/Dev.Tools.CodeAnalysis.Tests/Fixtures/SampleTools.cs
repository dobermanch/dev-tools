namespace Dev.Tools.CodeAnalysis.Tests.Fixtures;

/// <summary>
/// A miniature stand-in for the Dev.Tools library: the attributes, the tool interface and two tools
/// between them covering what the generators have to handle - a positional Args record with
/// defaults, an enum argument with described values, a piped input and output, a nullable property
/// and a result built with an object initializer.
///
/// The generator tests used to compile a stub whose ITool had no relation to the real one and whose
/// arguments were <c>int</c> and <c>long</c>, against a compilation that referenced nothing but
/// System.Private.CoreLib. Snapshots taken from that prove very little; these are close enough to a
/// real tool that a change in the emitted shape shows up.
/// </summary>
public static class SampleTools
{
    public const string Source =
        """
        using System;
        using System.Collections.Generic;
        using System.ComponentModel;
        using System.Threading;
        using System.Threading.Tasks;

        namespace Dev.Tools
        {
            [AttributeUsage(AttributeTargets.Class)]
            public sealed class ToolDefinitionAttribute : Attribute
            {
                public string Name { get; set; } = "";
                public string? Title { get; set; }
                public string? Description { get; set; }
                public string? Usage { get; set; }
                public string[] Aliases { get; set; } = [];
                public Category[] Categories { get; set; } = [];
                public Keyword[] Keywords { get; set; } = [];
            }

            [AttributeUsage(AttributeTargets.Property)]
            public sealed class PipeInputAttribute : Attribute;

            [AttributeUsage(AttributeTargets.Property)]
            public sealed class PipeOutputAttribute : Attribute;

            public enum Category { Converter, Text }

            public enum Keyword { Encode, Text }

            public enum ErrorCode { Unknown, TextEmpty }

            public record ToolResult
            {
                public IList<ErrorCode> ErrorCodes { get; init; } = new List<ErrorCode>();
            }

            public sealed class ToolException(ErrorCode errorCode) : Exception
            {
                public ErrorCode ErrorCode { get; } = errorCode;
            }

            public interface ITool;

            public interface ITool<in TArgs, TResult> : ITool
                where TResult : ToolResult
            {
                Task<TResult> RunAsync(TArgs args, CancellationToken cancellationToken);
            }
        }

        namespace Dev.Tools.Tools
        {
            [ToolDefinition(
                Name = "sample-encoder",
                Title = "Sample Encoder",
                Description = "Encodes text.\n\nA second paragraph the CLI list has no room for.",
                Usage = "Prefer this over hand-rolling an encoder.",
                Aliases = ["se"],
                Categories = [Category.Converter],
                Keywords = [Keyword.Encode, Keyword.Text]
            )]
            public sealed class SampleEncoderTool : ITool<SampleEncoderTool.Args, SampleEncoderTool.Result>
            {
                public Task<Result> RunAsync(Args args, CancellationToken cancellationToken)
                {
                    if (string.IsNullOrEmpty(args.Text))
                    {
                        throw new ToolException(ErrorCode.TextEmpty);
                    }

                    return Task.FromResult(new Result(args.Text));
                }

                public enum Padding
                {
                    [Description("Keep the trailing = characters")] Keep,
                    [Description("Strip the trailing = characters")] Strip
                }

                public sealed record Args(
                    [property: PipeInput, Description("Text to encode, read as UTF-8")]
                    string Text,
                    [property: Description("What to do with padding")]
                    Padding Padding = Padding.Keep,
                    [property: DisplayName("URL Safe"), Description("Use the URL-safe alphabet")]
                    bool UrlSafe = false);

                public sealed record Result(
                    [property: PipeOutput, DisplayName("Encoded Text"), Description("The encoded text")]
                    string Text) : ToolResult
                {
                    public Result() : this(string.Empty) { }
                }
            }

            [ToolDefinition(
                Name = "sample-parser",
                Title = "Sample Parser",
                Description = "Parses a value into its parts.",
                Categories = [Category.Text]
            )]
            public sealed class SampleParserTool : ITool<SampleParserTool.Args, SampleParserTool.Result>
            {
                public Task<Result> RunAsync(Args args, CancellationToken cancellationToken)
                    => Task.FromResult(new Result { Scheme = args.Value });

                public sealed record Args(
                    [property: PipeInput, Description("The value to parse")]
                    string Value);

                public sealed record Result : ToolResult
                {
                    [Description("Scheme of the parsed value, when it has one")]
                    public string? Scheme { get; init; }

                    [Description("Segments of the parsed value")]
                    public IReadOnlyList<string> Segments { get; init; } = [];
                }
            }
        }
        """;
}
