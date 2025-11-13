using System.Net.Mime;
using System.Text.Json.Nodes;
using Dev.Tools;
using Dev.Tools.Api.Core;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Scalar.AspNetCore;
using Microsoft.OpenApi;

[assembly: GenerateToolsApiEndpoint]

var builder = WebApplication.CreateBuilder(args);

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
    options.AddDocumentTransformer((document, context, ctx) => { return Task.CompletedTask; });

    options.AddOperationTransformer((operation, context, ctx) =>
    {
        foreach (var response in operation.Responses ?? [])
        {
            if (response.Value.Content != null && response.Value.Content.TryGetValue(MediaTypeNames.Application.Json, out var content))
            {
            }
        }

        return Task.CompletedTask;
    });

    options.AddSchemaTransformer((schema, context, ctx) =>
    {
        var type = context.JsonTypeInfo.Type;

        if (type is { Name: "Args", DeclaringType: not null })
        {
            var title = $"{type.DeclaringType.Name}Request";
            schema.Title = title;
            schema.Extensions?["x-schema-id"] = new JsonNodeExtension(JsonValue.Create(title));

        }

        if (type is { Name: "Result", DeclaringType: not null })
        {
            var title = $"{type.DeclaringType.Name}Response";
            schema.Title = title;
            schema.Extensions?["x-schema-id"] = new JsonNodeExtension(JsonValue.Create(title));

            schema.Properties?.Remove("hasErrors");
            schema.Properties?.Remove("errorCodes");
        }

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
    .Configure<IServer>((options, server) =>
    {
        options
            .WithTitle("Dev Tools API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.Servers = server.Features
            .Get<IServerAddressesFeature>()
            ?.Addresses
            .Select(it => new ScalarServer(it))
            .ToList();
    });

builder.Services
    .AddDevTools();

var app = builder.Build();
app.MapOpenApi();
app.MapScalarApiReference("ui/{documentName}");

app.MapGet("/ui", [ExcludeFromDescription]() => Results.Redirect("ui/v1", true, true));
app.MapControllers();
app.Run();