using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using REIT_Project.Services;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ReitContext>(options =>
    options.UseSqlServer(connectionString));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();
// builder.Services.AddControllers();  // no webpage fix

builder.Services.AddScoped<IAuditService, AuditService>();

// FIX 1: Tell the API how to handle loops in JSON responses
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// FIX 2: Tell the OpenAPI Generator to handle the REIT schema depth
builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        // This stops Scalar from trying to generate 
        // complex "Example" objects for every relationship.
        schema.Example = null; 
        return Task.CompletedTask;
    });
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    // Increases the limit so it can finish mapping the REIT relationships
    options.SerializerOptions.MaxDepth = 128; 
});

builder.Services.AddDbContext<ReitContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), 
    sqlOptions => {
        // This silences the warning by explicitly disabling savepoints,
        // which is fine because we roll back the whole transaction anyway.
        sqlOptions.EnableRetryOnFailure();
    })
    .ConfigureWarnings(w => w.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS))
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // This exposes the actual JSON file at /openapi/v1.json
    app.MapOpenApi(); 

    // This tells Scalar where to find that JSON file
    app.MapScalarApiReference(options => 
    {
        options.WithOpenApiRoutePattern("/openapi/v1.json");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapControllers();   // nowebpage fix
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}