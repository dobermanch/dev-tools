namespace Dev.Tools.Web.Core.Serializer;

public interface ISerializer<TS>
{
    TOut? Deserialize<TOut>(TS serializedData);
    TS Serialize<TIn>(TIn input);
}