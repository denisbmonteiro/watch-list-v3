using System.Text.Json.Serialization;
using WatchList.Application;
using WatchList.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddProblemDetails()
    .AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "WatchList API"));
}
else
{
    app.UseExceptionHandler();
}

app.UseStatusCodePages();

app.MapControllers();

app.Run();