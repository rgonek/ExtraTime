using System.Text.Json.Serialization;
using Azure.Identity;
using ExtraTime.Application;
using ExtraTime.Infrastructure;
using ExtraTime.Infrastructure.Data;
using ExtraTime.API.Features.Admin;
using ExtraTime.API.Features.Auth;
using ExtraTime.API.Features.Bots;
using ExtraTime.API.Features.DevTriggers;
using ExtraTime.API.Features.Football;
using ExtraTime.API.Features.Health;
using ExtraTime.API.Features.Leagues;
using ExtraTime.API.Features.Bets;
using Scalar.AspNetCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration in Production
if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVault:Name"];
    if (!string.IsNullOrEmpty(keyVaultName))
    {
        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
    }
}

// Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add OpenAPI document generation (required for Scalar)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info ??= new OpenApiInfo();
        document.Info.Title = "ExtraTime API";
        document.Info.Version = "v1";
        document.Info.Description = "ExtraTime Football Betting API";
        return Task.CompletedTask;
    });

    // Add JWT Bearer authentication support
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token"
        });

        document.Security ??= new List<OpenApiSecurityRequirement>();
        var requirement = new OpenApiSecurityRequirement();
        var schemeReference = new OpenApiSecuritySchemeReference("Bearer");
        requirement.Add(schemeReference, []);
        document.Security.Add(requirement);

        return Task.CompletedTask;
    });
});

// Clean Architecture services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Restore Aspire telemetry and health checks for the manually registered DbContext
builder.EnrichSqlServerDbContext<ApplicationDbContext>();

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

// Background jobs handled by Azure Functions project (ExtraTime.Functions)
// Database migrations are handled by the MigrationService project in Aspire

// Note: Health checks are configured by Aspire service defaults and AddSqlServerDbContext

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Map OpenAPI document endpoint (required for Scalar)
    app.MapOpenApi();

    // Configure Scalar UI
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("ExtraTime API")
               .WithTheme(ScalarTheme.Mars)
               .WithDefaultHttpClient(ScalarTarget.Http, ScalarClient.Http11)
               .WithModels(true)
               .WithDownloadButton(true);
    });

    // Map dev-only trigger endpoints for Aspire dashboard
    app.MapDevTriggerEndpoints();
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
app.MapBotEndpoints();
app.MapAdminBotEndpoints();
app.MapBetEndpoints();

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
