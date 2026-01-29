using System.Text.Json.Serialization;
using ExtraTime.Application;
using ExtraTime.Infrastructure;
using ExtraTime.Infrastructure.Data;
using ExtraTime.Infrastructure.Services;
using ExtraTime.API.Features.Admin;
using ExtraTime.API.Features.Auth;
using ExtraTime.API.Features.Football;
using ExtraTime.API.Features.Health;
using ExtraTime.API.Features.Leagues;
using ExtraTime.API.Features.Bets;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add Aspire SQL Server DbContext integration (when running under Aspire)
// Falls back to connection string from configuration when not running under Aspire
builder.AddSqlServerDbContext<ApplicationDbContext>("extratime");

// Configure JSON serialization - enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Clean Architecture services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Run database migrations on startup in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DatabaseMigrationService>();
}

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ExtraTime API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            []
        }
    });
});

// Note: Health checks are configured by Aspire service defaults and AddSqlServerDbContext

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Map endpoints
app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapFootballEndpoints();
app.MapFootballSyncEndpoints();
app.MapLeagueEndpoints();
app.MapBetEndpoints();

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
