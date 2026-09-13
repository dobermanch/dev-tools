using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dev.Tools.DataFormats;

/// <summary>
/// JSON, and the model every other format is read into and written from: objects, arrays, strings,
/// numbers, booleans and null.
/// </summary>
internal static class JsonFormat
{
    // Text other than JSON's own syntax is written as it is, not as \u escapes: é stays é.
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static readonly JsonSerializerOptions CompactOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static JsonNode? Read(string text)
    {
        try
        {
            // A key given twice is an error rather than the last one silently winning.
            return JsonNode.Parse(text, documentOptions: new JsonDocumentOptions { AllowDuplicateProperties = false });
        }
        catch (JsonException exception)
        {
            throw new UnreadableDataException("Not valid JSON", exception);
        }
    }

    public static string Write(JsonNode? data) => data?.ToJsonString(IndentedOptions) ?? "null";
}
