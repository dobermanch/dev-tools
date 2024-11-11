using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dev.Tools.Web.Core.Serializer;

internal sealed class DefaultJsonSerializer: IJsonSerializer
{
    private readonly JsonSerializerOptions? _options = new ()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    public TOut? Deserialize<TOut>(string input)
    {
        return JsonSerializer.Deserialize<TOut>(input, _options);
    }

    public string Serialize<TIn>(TIn input)
    {
        return JsonSerializer.Serialize(input, _options);
    }
}