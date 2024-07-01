using Swashbuckle.AspNetCore.SwaggerGen;

namespace Dev.Tools.Api.Core;

public static class SwaggerGenOptionsExtensions
{
    public static void UseApiEndpoints(this SwaggerGenOptions options)
    {
        options.UseInlineDefinitionsForEnums();
        options.TagActionsBy(api => ["Tools"]);
        options.CustomSchemaIds(t =>
        {
            if (t.DeclaringType is not null)
            {
                return $"{t.DeclaringType.Name.Replace("Endpoint", string.Empty).Replace("Tool", string.Empty)}.{t.Name.Replace("Dto", string.Empty)}";
            }

            return t.Name;
        });
    }
}
