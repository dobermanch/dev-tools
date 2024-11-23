using Dev.Tools;
using Dev.Tools.Api.Core;
using Dev.Tools.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

[assembly:GenerateApiEndpoints]

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
builder.Services.AddSwaggerGen(c => {
    c.UseApiEndpoints();
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Dev Tools", Version = "v1" });
    c.EnableAnnotations();
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressInferBindingSourcesForParameters = true;
});

builder.Services
    .AddTransient<Base64DecoderTool>()
    .AddTransient<Base64EncoderTool>()
    .AddTransient<UuidGeneratorTool>()
    ;

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.Run();

