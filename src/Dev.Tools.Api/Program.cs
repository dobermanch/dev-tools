using System.Text.Json.Nodes;
using Dev.Tools;
using Dev.Tools.Api.Core;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Scalar.AspNetCore;

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
    options.CreateSchemaReferenceId = jsonTypeInfo =>
    {
        var type = jsonTypeInfo.Type;
        return type switch
        {
            { Name: "Args", DeclaringType: not null } => $"{type.DeclaringType.Name}Request",
            { Name: "Result", DeclaringType: not null } => $"{type.DeclaringType.Name}Response",
            _ => type.Name
        };
    };

    options.AddSchemaTransformer((schema, context, ctx) =>
    {
        var type = context.JsonTypeInfo.Type;

        // Remove internal properties from Result types
        if (type is { Name: "Result", DeclaringType: not null })
        {
            schema.Properties?.Remove("hasErrors");
            schema.Properties?.Remove("errorCodes");
        }

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
app.MapScalarApiReference("ui");

app.MapGet("/ui", [ExcludeFromDescription]() => Results.Redirect("ui/v1", true, true));
app.MapControllers();
app.Run();