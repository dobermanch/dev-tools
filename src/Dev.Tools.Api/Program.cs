using System.Text.Json.Nodes;
using Dev.Tools;
using Dev.Tools.Api.Core;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Dev.Tools.Localization;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

[assembly: GenerateToolsApiEndpoint]

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services
    .AddControllers(options =>
    {
        options.UseNamespaceRouteToken();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, ctx) =>
    {
        var type = context.JsonTypeInfo.Type;

        // Customize enum values
        if (type.IsEnum)
        {
            schema.Enum = Enum.GetNames(type)
                .Select(it => JsonValue.Create(it))
                .OfType<JsonNode>()
                .ToList();
        }

        return Task.CompletedTask;
    });
});

builder.Services
    .Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressInferBindingSourcesForParameters = true;
    })
    .AddOptions<ScalarOptions>()
    .Configure(options =>
    {
        options.Title = "Dev Tools API";
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

builder.Services
    .AddDevTools()
    .AddDevToolsLocalization();

var app = builder.Build();
app.UseForwardedHeaders();

var localizationProvider = app.Services.GetRequiredService<ILocalizationProvider>();
var supportedCultures = localizationProvider.SupportedCultures.Select(c => c.Name).ToArray();
app.UseRequestLocalization(options =>
{
    options.SetDefaultCulture(LocalizationProvider.FallbackCulture)
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});

app.MapOpenApi();
app.MapScalarApiReference("ui");

app.MapGet("/ui", [ExcludeFromDescription]() => Results.Redirect("ui/v1", true, true));
app.MapControllers();
app.Run();