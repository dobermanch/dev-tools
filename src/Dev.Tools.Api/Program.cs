using System.Text.Json.Nodes;
using Dev.Tools;
using Dev.Tools.Api.Core;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
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

builder.Services.AddDevTools();

// An unexpected exception is logged and answered with a detail-free RFC 9457 problem, instead of its
// message reaching the client. Tool errors never get this far - endpoints return those themselves.
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.UseForwardedHeaders();

app.MapOpenApi();
app.MapScalarApiReference("ui");

app.MapGet("/ui", [ExcludeFromDescription]() => Results.Redirect("ui/v1", true, true));
app.MapControllers();
app.Run();