using System.Net.Mime;
using Dev.Tools;
using Dev.Tools.Api.Core;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.OpenApi.Any;
using Scalar.AspNetCore;

[assembly: GenerateApiEndpoints]

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
        foreach (var response in operation.Responses)
        {
            if (response.Value.Content.TryGetValue(MediaTypeNames.Application.Json, out var content))
            {
            }
        }

        return Task.CompletedTask;
    });

    options.AddSchemaTransformer((schema, context, ctx) =>
    {
        if (context.JsonTypeInfo.Type is { Name: "Args", DeclaringType: not null })
        {
            schema.Annotations["x-schema-id"] =
                schema.Title = $"{context.JsonTypeInfo.Type.DeclaringType.Name}Request";
        }

        if (context.JsonTypeInfo.Type is { Name: "Result", DeclaringType: not null })
        {
            schema.Annotations["x-schema-id"] =
                schema.Title = $"{context.JsonTypeInfo.Type.DeclaringType.Name}Response";

            schema.Properties.Remove("hasErrors");
            schema.Properties.Remove("errorCodes");
        }

        if (context.JsonTypeInfo.Type.IsEnum)
        {
            schema.Enum =
                Enum.GetNames(context.JsonTypeInfo.Type)
                    .Select(it => (IOpenApiAny)new OpenApiString(it))
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
            .WithEndpointPrefix("ui/{documentName}")
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
app.MapScalarApiReference();

app.MapGet("/ui", [ExcludeFromDescription]() => Results.Redirect("ui/v1", true, true));
app.MapControllers();
app.Run();